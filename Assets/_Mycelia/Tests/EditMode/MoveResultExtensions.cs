using System.Collections.Generic;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;

namespace Mycelia.Domain.Tests
{
    /// <summary>Readable (allocating) views of a <see cref="MoveResult"/>, for assertions only.</summary>
    public static class MoveResultExtensions
    {
        public static List<MovePhase> Phases(this MoveResult result)
        {
            var phases = new List<MovePhase>();
            for (int i = 0; i < result.PhaseCount; i++)
            {
                phases.Add(result.GetPhase(i).Phase);
            }
            return phases;
        }

        public static List<TStep> Steps<TStep>(this MoveResult result, MovePhase phase) where TStep : struct, IMoveStep
        {
            var steps = new List<TStep>();
            for (int p = 0; p < result.PhaseCount; p++)
            {
                PhaseSteps phaseSteps = result.GetPhase(p);
                if (phaseSteps.Phase != phase)
                    continue;

                for (int s = phaseSteps.Start; s < phaseSteps.Start + phaseSteps.Count; s++)
                {
                    if (result.TryGet(result.GetStep(s), out TStep step))
                        steps.Add(step);
                }
            }
            return steps;
        }

        /// <summary>Any two neighbouring cells holding the same spore type, for tests that just need a legal move.</summary>
        public static (GridPosition First, GridPosition Second) FindPair(IReadOnlyBoard board)
        {
            for (int column = 0; column < board.Width; column++)
            {
                for (int row = 0; row < board.Height; row++)
                {
                    var cell = new GridPosition(column, row);
                    if (!board.IsPlayable(cell)) continue;
                    foreach (var (dc, dr) in new[] { (1, 0), (0, 1), (1, 1), (-1, 1) })
                    {
                        var other = new GridPosition(column + dc, row + dr);
                        if (board.IsPlayable(other) && board.GetOccupant(other) == board.GetOccupant(cell))
                            return (cell, other);
                    }
                }
            }
            throw new System.InvalidOperationException("The board has no valid chain.");
        }
    }
}
