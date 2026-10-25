using System;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Sessions;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// Shows a win on the board before the result (Soni, 2026-10-25): every spore left pops in a wave spreading out from
    /// where the last chain ended, then the outcome reaches the next listener (the HUD, which shows the result). Moves,
    /// goals and losses pass straight through. While the wave plays, a screen-wide button skips to the result.
    /// The wave runs after the level has ended, when it can no longer be paused (ADR-0013).
    /// </summary>
    public sealed class WinWave : MonoBehaviour, IPlaybackListener
    {
        [SerializeField, Tooltip("Covers the screen while the wave plays; a tap skips to the result.")]
        Button skip;
        [SerializeField, Min(0), Tooltip("Seconds the wave takes to travel one cell.")]
        float stepDelay = 0.06f;
        [SerializeField, Min(1), Tooltip("How large a spore grows at the top of its pop.")]
        float popScale = 1.25f;
        [SerializeField, Min(0.01f), Tooltip("Seconds one spore takes to grow and spring back.")]
        float popDuration = 0.36f;
        [SerializeField, Min(0), Tooltip("Seconds between the last pop and the result.")]
        float hold = 0.3f;

        BoardView boardView;
        int columns;
        int rows;
        Func<GridPosition> origin;
        IPlaybackListener next;
        Sequence sequence;
        SessionState outcome;
        int stars;

        public bool IsPlaying { get; private set; }

        /// <param name="origin">Where the wave starts: the cell the last chain ended on.</param>
        public void Init(BoardView boardView, int columns, int rows, Func<GridPosition> origin, IPlaybackListener next)
        {
            this.boardView = boardView;
            this.columns = columns;
            this.rows = rows;
            this.origin = origin ?? throw new ArgumentNullException(nameof(origin));
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            skip.onClick.RemoveAllListeners();
            skip.onClick.AddListener(Skip);
            skip.gameObject.SetActive(false);
        }

        public void MovesChanged(int remaining) => next.MovesChanged(remaining);

        public void ObjectiveProgressed(int index, int current, int target) => next.ObjectiveProgressed(index, current, target);

        public void SessionEnded(SessionState outcome, int stars)
        {
            if (outcome != SessionState.Won)
            {
                next.SessionEnded(outcome, stars);
                return;
            }

            this.outcome = outcome;
            this.stars = stars;
            IsPlaying = true;
            skip.gameObject.SetActive(true);

            Vector2 start = new Vector2(origin().Column, origin().Row);
            float last = 0;
            sequence = Sequence.Create();
            for (int column = 0; column < columns; column++)
            for (int row = 0; row < rows; row++)
            {
                SporeView spore = boardView.SporeAt(new GridPosition(column, row));
                if (spore == null)
                    continue;
                float delay = Vector2.Distance(start, new Vector2(column, row)) * stepDelay;
                last = Mathf.Max(last, delay);
                sequence.Insert(delay, Tween.Scale(spore.transform, 1f, popScale, popDuration / 2, Ease.OutQuad, cycles: 2, CycleMode.Yoyo));
            }
            sequence.InsertCallback(last + popDuration + hold, this, static wave => wave.Finish());
        }

        /// <summary>Jumps to the result: every spore back to size, the outcome passed on now. Does nothing once done.</summary>
        public void Skip()
        {
            if (IsPlaying && sequence.isAlive)
                sequence.Complete();
        }

        void Finish()
        {
            IsPlaying = false;
            skip.gameObject.SetActive(false);
            next.SessionEnded(outcome, stars);
        }

        void OnDestroy()
        {
            // No callback may reach a destroyed view (ADR-0009 rule 5).
            if (sequence.isAlive)
                sequence.Stop();
        }
    }
}
