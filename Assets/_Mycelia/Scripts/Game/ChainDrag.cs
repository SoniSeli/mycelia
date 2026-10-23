using System;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Sessions;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Turns press, move and release of one pointer, in board coordinates, into chain touches and a committed move
    /// (ADR-0008). Engine-input-free so it can be tested directly; <see cref="ChainInputController"/> feeds it.
    /// Allocation-free while dragging.
    /// </summary>
    public sealed class ChainDrag
    {
        readonly LevelSession session;
        readonly IChainFeedback feedback;
        readonly float hitRadius;
        readonly float sampleStep;
        Vector2 lastPoint;

        public ChainDrag(LevelSession session, IChainFeedback feedback, float hitRadius, float sampleStep)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            if (hitRadius <= 0 || hitRadius > 0.5f)
                throw new ArgumentOutOfRangeException(nameof(hitRadius), hitRadius, "The hit radius must be in (0, 0.5] cells.");
            if (sampleStep <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleStep), sampleStep, "The sample step must be positive.");

            this.hitRadius = hitRadius;
            this.sampleStep = sampleStep;
        }

        public bool IsDragging { get; private set; }

        public void Press(Vector2 boardPoint)
        {
            if (session.State != SessionState.Playing)
                return;

            IsDragging = true;
            lastPoint = boardPoint;
            TouchAt(boardPoint);
        }

        public void Move(Vector2 boardPoint)
        {
            if (!IsDragging)
                return;

            foreach (Vector2 point in CellHitTest.SamplePoints(lastPoint, boardPoint, sampleStep))
                TouchAt(point);
            lastPoint = boardPoint;
        }

        /// <summary>Commits the chain if it is long enough, otherwise cancels it.</summary>
        /// <returns>The move to play back, or null if nothing was committed. Valid until the next commit.</returns>
        public MoveResult Release()
        {
            if (!IsDragging)
                return null;

            IsDragging = false;
            MoveResult result = null;
            if (session.Chain.IsCommittable)
                result = session.Commit();
            else
                session.Chain.Clear();

            feedback.ChainEnded(session.Chain);
            return result;
        }

        void TouchAt(Vector2 point)
        {
            IReadOnlyBoard board = session.Board;
            if (!CellHitTest.TryHit(point, board.Width, board.Height, hitRadius, out GridPosition cell))
                return;

            ChainChange change = session.Chain.Touch(cell);
            if (change != ChainChange.Unchanged)
                feedback.ChainChanged(session.Chain, change, cell);
        }
    }
}
