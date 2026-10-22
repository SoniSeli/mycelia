using System.Collections.Generic;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Objectives;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Moves
{
    /// <summary>One phase of a move's resolution. New mechanics add rules; existing rules don't change (ADR-0004).</summary>
    public interface IMoveRule
    {
        MovePhase Phase { get; }
        void Apply(MoveContext context);
    }

    /// <summary>What rules can read and change while a move resolves. One instance per session, reused for every move.</summary>
    public sealed class MoveContext
    {
        public Board Board { get; }
        public IRandom Random { get; }
        public LevelConfig Config { get; }
        public IReadOnlyList<IObjective> Objectives { get; }
        public MoveBudget Moves { get; }
        public MoveResult Result { get; }

        /// <summary>The committed chain for the move being resolved.</summary>
        public IReadOnlyList<GridPosition> Chain { get; internal set; }

        public MoveContext(
            Board board,
            IRandom random,
            LevelConfig config,
            IReadOnlyList<IObjective> objectives,
            MoveBudget moves,
            MoveResult result)
        {
            Board = board;
            Random = random;
            Config = config;
            Objectives = objectives;
            Moves = moves;
            Result = result;
        }

        public void Record<TStep>(in TStep step) where TStep : struct, IMoveStep => Result.Record(step);
    }
}
