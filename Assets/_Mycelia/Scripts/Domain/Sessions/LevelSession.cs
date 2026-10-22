using System;
using System.Collections.Generic;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Objectives;
using Mycelia.Domain.Randomness;

namespace Mycelia.Domain.Sessions
{
    /// <summary>
    /// One attempt at a level: the board, the chain being dragged, objectives and moves (ADR-0003).
    /// <see cref="Commit"/> resolves a move through the rule pipeline and returns what happened for playback.
    /// </summary>
    public sealed class LevelSession
    {
        readonly Board board;
        readonly IObjective[] objectives;
        readonly MoveBudget moves;
        readonly MoveResult result;
        readonly MoveContext context;
        readonly RulePipeline pipeline;

        /// <param name="startingBoard">
        /// An exact opening board (for tests, or a hand-made tutorial opening). When omitted, the board is filled
        /// from <paramref name="random"/> and is guaranteed to contain a valid chain (ADR-0007 draw order).
        /// </param>
        public LevelSession(LevelConfig config, IRandom random, IReadOnlyBoard startingBoard = null)
        {
            board = new Board(config.Width, config.Height);
            int cells = config.Width * config.Height;

            if (startingBoard != null)
                CopyBoard(startingBoard, board);
            else
                FillStartingBoard(config, random, cells);

            objectives = config.CreateObjectives();
            moves = new MoveBudget(config.Moves);
            result = new MoveResult(cells);
            context = new MoveContext(board, random, config, objectives, moves, result);
            pipeline = RulePipeline.For(config);
            Chain = new Chain(board);
            Config = config;
        }

        public LevelConfig Config { get; }
        public IReadOnlyBoard Board => board;

        /// <summary>The chain the player is dragging; input calls <see cref="Chains.Chain.Touch"/> on it.</summary>
        public Chain Chain { get; }

        public IReadOnlyList<IObjective> Objectives => objectives;
        public int MovesRemaining => moves.Remaining;
        public SessionState State { get; private set; } = SessionState.Playing;

        /// <summary>1–3 once the level is won, otherwise 0.</summary>
        public int Stars { get; private set; }

        /// <summary>
        /// Resolves the current chain as a move. The returned result is reused: it is valid only until the next commit.
        /// </summary>
        public MoveResult Commit()
        {
            if (State != SessionState.Playing)
                throw new InvalidOperationException($"The level is already {State}.");
            if (!Chain.IsCommittable)
                throw new InvalidOperationException($"A move needs a chain of at least {Chain.MinimumLength} spores.");

            context.Chain = Chain.Cells;
            pipeline.Run(context);
            Chain.Clear();

            State = result.Outcome;
            Stars = result.Stars;
            return result;
        }

        void FillStartingBoard(LevelConfig config, IRandom random, int cells)
        {
            Refill.Apply(board, config.SporeTypes, random, new List<SporeSpawned>(cells));
            new Reshuffler(board).Apply(config.SporeTypes, random,
                new List<SporeRelocated>(), new List<SporeCleared>(), new List<SporeSpawned>());
        }

        static void CopyBoard(IReadOnlyBoard source, Board target)
        {
            if (source.Width != target.Width || source.Height != target.Height)
                throw new ArgumentException(
                    $"The starting board is {source.Width} × {source.Height}, but the level is {target.Width} × {target.Height}.");

            for (int column = 0; column < source.Width; column++)
            {
                for (int row = 0; row < source.Height; row++)
                {
                    var cell = new GridPosition(column, row);
                    if (source.IsPlayable(cell))
                        target.SetOccupant(cell, source.GetOccupant(cell));
                    else
                        target.SetVoid(cell);
                }
            }
        }
    }
}
