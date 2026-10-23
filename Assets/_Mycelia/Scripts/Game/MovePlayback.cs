using System;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using PrimeTween;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Plays a <see cref="MoveResult"/> as one PrimeTween sequence (ADR-0009): phases one after another, the steps in
    /// a phase together. The board view's cells are updated to the move's end state as the sequence is built, and the
    /// tweens animate the views there. Allocation-free after <see cref="Init"/>: no capturing lambdas, buffers sized
    /// up front, and tween capacity reserved.
    /// </summary>
    public sealed class MovePlayback : MonoBehaviour, IMovePlayer, IPausable
    {
        [SerializeField] PlaybackSettings settings;

        BoardView boardView;
        int rows;
        Sequence sequence;

        // Views leaving the board, returned to the pool when their phase ends. phaseEnds marks each phase's last one.
        SporeView[] leaving;
        int leavingCount;
        int returnedCount;
        int[] phaseEnds;
        int phaseEndCount;
        int nextPhaseEnd;

        // Views moving within the board in the current phase: taken off their cells first, then placed.
        SporeView[] moving;
        GridPosition[] movingFrom;
        GridPosition[] movingTo;
        bool[] movingFalls;
        int movingCount;

        // What the listener hears about, held until the matching moment in the sequence.
        IPlaybackListener listener;
        readonly ObjectiveProgressed[] progress = new ObjectiveProgressed[16];
        int progressCount;
        SessionEnded ending;
        bool ends;

        public bool IsPlaying { get; private set; }

        public void Init(BoardView boardView, int columns, int rows, IPlaybackListener listener)
        {
            this.boardView = boardView;
            this.rows = rows;
            this.listener = listener ?? throw new ArgumentNullException(nameof(listener));
            int cells = columns * rows;
            // A move can clear the chain and, if the board is regenerated, every cell again.
            leaving = new SporeView[2 * cells];
            phaseEnds = new int[16];
            moving = new SporeView[cells];
            movingFrom = new GridPosition[cells];
            movingTo = new GridPosition[cells];
            movingFalls = new bool[cells];
            PrimeTweenConfig.SetTweensCapacity(settings.TweenCapacity);
        }

        public void Play(MoveResult move)
        {
            if (IsPlaying)
                throw new InvalidOperationException("A move is already playing; input should have been locked.");

            IsPlaying = true;
            leavingCount = returnedCount = phaseEndCount = nextPhaseEnd = 0;
            CollectReports(move);
            listener.MovesChanged(move.MovesRemaining);
            sequence = Sequence.Create();

            float time = 0;
            float popEnd = 0;
            for (int i = 0; i < move.PhaseCount; i++)
            {
                time += PlayPhase(move, move.GetPhase(i), time);
                // The first phase is always the chain's Clear phase (ADR-0004): goals count up as its spores finish popping.
                if (i == 0) popEnd = time;
            }

            if (progressCount > 0)
                sequence.InsertCallback(popEnd, this, static playback => playback.ReportProgress());
            sequence.InsertCallback(time, this, static playback => playback.Finish());
        }

        void CollectReports(MoveResult move)
        {
            progressCount = 0;
            ends = false;
            for (int p = 0; p < move.PhaseCount; p++)
            {
                PhaseSteps phase = move.GetPhase(p);
                for (int i = phase.Start; i < phase.Start + phase.Count; i++)
                {
                    StepRef step = move.GetStep(i);
                    if (move.TryGet(step, out ObjectiveProgressed progressed))
                        progress[progressCount++] = progressed;
                    else if (move.TryGet(step, out SessionEnded ended))
                    {
                        ending = ended;
                        ends = true;
                    }
                }
            }
        }

        void ReportProgress()
        {
            for (int i = 0; i < progressCount; i++)
                listener.ObjectiveProgressed(progress[i].ObjectiveIndex, progress[i].Current, progress[i].Target);
        }

        /// <summary>Jumps to the end of the current move, running every callback now.</summary>
        public void CompleteNow()
        {
            if (sequence.isAlive)
                sequence.Complete();
        }

        /// <summary>Freezes the move being played under the pause overlay; input stays locked until it finishes.</summary>
        public void SetPaused(bool paused)
        {
            if (sequence.isAlive)
                sequence.isPaused = paused;
        }

        void OnDestroy()
        {
            // No callback may reach a destroyed view (ADR-0009 rule 5).
            if (sequence.isAlive)
                sequence.Stop();
        }

        float PlayPhase(MoveResult move, PhaseSteps phase, float start)
        {
            float duration = 0;
            int leavingStart = leavingCount;
            movingCount = 0;

            // Take every view off the cell it leaves, so a cell can be refilled in the same phase.
            for (int i = phase.Start; i < phase.Start + phase.Count; i++)
            {
                StepRef step = move.GetStep(i);
                if (move.TryGet(step, out SporeCleared cleared))
                {
                    SporeView view = boardView.Take(cleared.Cell);
                    leaving[leavingCount++] = view;
                    Color colour = view.Renderer.color;
                    sequence.Insert(start, Tween.Scale(view.transform, 1f, 0f, settings.ClearDuration, settings.ClearEase));
                    sequence.Insert(start, Tween.Color(view.Renderer, colour, new Color(colour.r, colour.g, colour.b, 0f), settings.ClearDuration));
                    duration = Mathf.Max(duration, settings.ClearDuration);
                }
                else if (move.TryGet(step, out SporeFell fell))
                {
                    TakeMoving(fell.From, fell.To, falls: true);
                    duration = Mathf.Max(duration, settings.FallDuration(fell.From.Row - fell.To.Row));
                }
                else if (move.TryGet(step, out SporeRelocated relocated))
                {
                    TakeMoving(relocated.From, relocated.To, falls: false);
                    duration = Mathf.Max(duration, settings.RelocateDuration);
                }
            }

            // Then place moved and new views on their cells and animate them there.
            for (int i = 0; i < movingCount; i++)
            {
                SporeView view = moving[i];
                boardView.Place(movingTo[i], view);
                bool fall = movingFalls[i];
                float tweenDuration = fall ? settings.FallDuration(movingFrom[i].Row - movingTo[i].Row) : settings.RelocateDuration;
                sequence.Insert(start, Tween.LocalPosition(view.transform,
                    BoardLayout.CellCentre(movingFrom[i]), BoardLayout.CellCentre(movingTo[i]),
                    tweenDuration, fall ? settings.FallEase : settings.RelocateEase));
            }

            for (int i = phase.Start; i < phase.Start + phase.Count; i++)
            {
                if (!move.TryGet(move.GetStep(i), out SporeSpawned spawned))
                    continue;

                // New spores start above the board, stacked by drop slot, and fade in as they fall into place.
                int dropRow = rows + spawned.DropSlot;
                SporeView view = boardView.Pool.Rent();
                view.Show(boardView.Catalog.TypeOf(spawned.Type), BoardLayout.CellCentre(new GridPosition(spawned.Cell.Column, dropRow)));
                Color colour = view.Renderer.color;
                view.Renderer.color = new Color(colour.r, colour.g, colour.b, 0f);
                boardView.Place(spawned.Cell, view);

                float fallDuration = settings.FallDuration(dropRow - spawned.Cell.Row);
                sequence.Insert(start, Tween.LocalPosition(view.transform,
                    view.transform.localPosition, BoardLayout.CellCentre(spawned.Cell), fallDuration, settings.FallEase));
                sequence.Insert(start, Tween.Color(view.Renderer, view.Renderer.color, colour, Mathf.Min(settings.SpawnFadeDuration, fallDuration)));
                duration = Mathf.Max(duration, fallDuration);
            }

            if (leavingCount > leavingStart)
            {
                phaseEnds[phaseEndCount++] = leavingCount;
                sequence.InsertCallback(start + duration, this, static playback => playback.ReturnLeavingViews());
            }

            return duration;
        }

        void TakeMoving(GridPosition from, GridPosition to, bool falls)
        {
            moving[movingCount] = boardView.Take(from);
            movingFrom[movingCount] = from;
            movingTo[movingCount] = to;
            movingFalls[movingCount] = falls;
            movingCount++;
        }

        // Returns the views cleared in the phase that just ended.
        void ReturnLeavingViews()
        {
            int end = phaseEnds[nextPhaseEnd++];
            for (; returnedCount < end; returnedCount++)
            {
                boardView.Pool.Return(leaving[returnedCount]);
                leaving[returnedCount] = null;
            }
        }

        void Finish()
        {
            IsPlaying = false;
            if (ends)
                listener.SessionEnded(ending.Outcome, ending.Stars);
        }
    }
}
