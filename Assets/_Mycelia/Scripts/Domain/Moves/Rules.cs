using System.Collections.Generic;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Moves
{
    // The v1 rules (ADR-0004). Each wraps a tested rule function and records what it did.
    // Their lists are sized once per level and reused, so resolving a move does not allocate.

    sealed class ClearChainRule : IMoveRule
    {
        readonly List<SporeCleared> cleared;

        public ClearChainRule(int cells) => cleared = new List<SporeCleared>(cells);

        public MovePhase Phase => MovePhase.Clear;

        public void Apply(MoveContext context)
        {
            cleared.Clear();
            ClearChain.Apply(context.Board, context.Chain, cleared);
            for (int i = 0; i < cleared.Count; i++)
            {
                context.Record(cleared[i]);
            }
        }
    }

    sealed class GravityRule : IMoveRule
    {
        readonly List<SporeFell> falls;

        public GravityRule(int cells) => falls = new List<SporeFell>(cells);

        public MovePhase Phase => MovePhase.Gravity;

        public void Apply(MoveContext context)
        {
            falls.Clear();
            Gravity.Apply(context.Board, falls);
            for (int i = 0; i < falls.Count; i++)
            {
                context.Record(falls[i]);
            }
        }
    }

    sealed class RefillRule : IMoveRule
    {
        readonly List<SporeSpawned> spawned;

        public RefillRule(int cells) => spawned = new List<SporeSpawned>(cells);

        public MovePhase Phase => MovePhase.Refill;

        public void Apply(MoveContext context)
        {
            spawned.Clear();
            Refill.Apply(context.Board, context.Config.SporeTypes, context.Random, spawned);
            for (int i = 0; i < spawned.Count; i++)
            {
                context.Record(spawned[i]);
            }
        }
    }

    /// <summary>Settle phase: the reshuffle guarantee (design §3.3 step 5).</summary>
    sealed class ReshuffleRule : IMoveRule
    {
        readonly List<SporeRelocated> relocated;
        readonly List<SporeCleared> cleared;
        readonly List<SporeSpawned> spawned;
        readonly Reshuffler reshuffler;

        // Built with the session, not on the first move: a level's first move must not allocate either.
        public ReshuffleRule(Board board, int cells)
        {
            reshuffler = new Reshuffler(board);
            relocated = new List<SporeRelocated>(cells);
            cleared = new List<SporeCleared>(cells);
            spawned = new List<SporeSpawned>(cells);
        }

        public MovePhase Phase => MovePhase.Settle;

        public void Apply(MoveContext context)
        {
            relocated.Clear();
            cleared.Clear();
            spawned.Clear();

            reshuffler.Apply(context.Config.SporeTypes, context.Random, relocated, cleared, spawned);

            for (int i = 0; i < relocated.Count; i++) context.Record(relocated[i]);
            for (int i = 0; i < cleared.Count; i++) context.Record(cleared[i]);
            for (int i = 0; i < spawned.Count; i++) context.Record(spawned[i]);
        }
    }

    /// <summary>A committed chain costs one move (design §3.3 step 4); a reshuffle never does.</summary>
    sealed class MoveCounterRule : IMoveRule
    {
        public MovePhase Phase => MovePhase.Evaluate;

        public void Apply(MoveContext context)
        {
            context.Moves.Spend();
            context.Result.MovesRemaining = context.Moves.Remaining;
        }
    }

    /// <summary>
    /// Counts only spores cleared by the player's chain (the Clear phase), never spores removed
    /// when a dead board is regenerated in the Settle phase.
    /// </summary>
    sealed class ObjectivesRule : IMoveRule
    {
        readonly bool[] changed;

        public ObjectivesRule(int objectiveCount) => changed = new bool[objectiveCount];

        public MovePhase Phase => MovePhase.Evaluate;

        public void Apply(MoveContext context)
        {
            MoveResult result = context.Result;
            var objectives = context.Objectives;
            for (int i = 0; i < changed.Length; i++) changed[i] = false;

            for (int p = 0; p < result.PhaseCount; p++)
            {
                PhaseSteps phase = result.GetPhase(p);
                if (phase.Phase != MovePhase.Clear)
                    continue;

                for (int s = phase.Start; s < phase.Start + phase.Count; s++)
                {
                    if (!result.TryGet(result.GetStep(s), out SporeCleared cleared))
                        continue;

                    for (int o = 0; o < objectives.Count; o++)
                    {
                        if (objectives[o].Apply(cleared))
                            changed[o] = true;
                    }
                }
            }

            for (int o = 0; o < objectives.Count; o++)
            {
                if (changed[o])
                    context.Record(new ObjectiveProgressed(o, objectives[o].Current, objectives[o].Target));
            }
        }
    }

    /// <summary>Win, lose or keep playing (design §3.5), and stars for a win (design §3.4).</summary>
    sealed class OutcomeRule : IMoveRule
    {
        public MovePhase Phase => MovePhase.Evaluate;

        public void Apply(MoveContext context)
        {
            SessionState state = SessionOutcome.Evaluate(context.Objectives, context.Moves);
            int stars = state == SessionState.Won
                ? StarRating.For(context.Moves.Remaining, context.Moves.Starting, context.Config.StarThresholds)
                : 0;

            context.Result.Outcome = state;
            context.Result.Stars = stars;
            if (state != SessionState.Playing)
                context.Record(new SessionEnded(state, stars));
        }
    }
}
