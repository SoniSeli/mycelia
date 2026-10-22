using System.Collections.Generic;
using System.Linq;
using Mycelia.Domain.Levels;

namespace Mycelia.Domain.Moves
{
    /// <summary>
    /// Runs a move's rules phase by phase (ADR-0004). Rules in the same phase run in the order they were given.
    /// The pipeline for a level is assembled in one place, <see cref="For"/>.
    /// </summary>
    public sealed class RulePipeline
    {
        readonly IMoveRule[] rules;

        public RulePipeline(IEnumerable<IMoveRule> rules)
        {
            // OrderBy is a stable sort, so rules keep their given order within a phase.
            this.rules = rules.OrderBy(rule => rule.Phase).ToArray();
        }

        public IReadOnlyList<IMoveRule> Rules => rules;

        /// <summary>The v1 rules. A new mechanic registers its rules here.</summary>
        public static RulePipeline For(LevelConfig config)
        {
            int cells = config.Width * config.Height;
            return new RulePipeline(new IMoveRule[]
            {
                new ClearChainRule(cells),
                new GravityRule(cells),
                new RefillRule(cells),
                new ReshuffleRule(cells),
                new MoveCounterRule(),
                new ObjectivesRule(config.Objectives.Count),
                new OutcomeRule()
            });
        }

        public void Run(MoveContext context)
        {
            MoveResult result = context.Result;
            result.Reset();

            int next = 0;
            for (var phase = MovePhase.Clear; phase <= MovePhase.Evaluate; phase++)
            {
                result.BeginPhase(phase);
                while (next < rules.Length && rules[next].Phase == phase)
                {
                    rules[next].Apply(context);
                    next++;
                }
                result.EndPhase();
            }
        }
    }
}
