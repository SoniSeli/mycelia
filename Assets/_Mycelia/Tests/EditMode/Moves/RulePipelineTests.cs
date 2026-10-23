using System.Linq;
using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Tests.Moves
{
    public class RulePipelineTests
    {
        static LevelConfig Config() =>
            new LevelConfig("level-test", 6, 6, 20, new[] { Boards.IdOf('A'), Boards.IdOf('B') },
                new[] { new ObjectiveDefinition(Boards.IdOf('A'), 10) });

        [Test]
        public void TheV1Pipeline_RunsItsRulesInTheDocumentedOrder()
        {
            // ADR-0004 phase table. Changing this order changes every seed's game (ADR-0007).
            var rules = RulePipeline.For(Config(), new Board(6, 6)).Rules;

            Assert.That(rules.Select(r => r.GetType().Name), Is.EqualTo(new[]
            {
                "ClearChainRule", "GravityRule", "RefillRule", "ReshuffleRule",
                "MoveCounterRule", "ObjectivesRule", "OutcomeRule"
            }));
            Assert.That(rules.Select(r => r.Phase), Is.EqualTo(new[]
            {
                MovePhase.Clear, MovePhase.Gravity, MovePhase.Refill, MovePhase.Settle,
                MovePhase.Evaluate, MovePhase.Evaluate, MovePhase.Evaluate
            }));
        }

        [Test]
        public void Rules_AreSortedByPhase_KeepingTheirOrderWithinAPhase()
        {
            var late = new StubRule(MovePhase.Evaluate);
            var early = new StubRule(MovePhase.Clear);
            var lateToo = new StubRule(MovePhase.Evaluate);

            var pipeline = new RulePipeline(new IMoveRule[] { late, early, lateToo });

            Assert.That(pipeline.Rules, Is.EqualTo(new IMoveRule[] { early, late, lateToo }));
        }

        [Test]
        public void TryGet_ReturnsFalseForAStepOfAnotherType()
        {
            var session = new LevelSession(Config(), new Pcg32(seed: 1));
            var (a, b) = MoveResultExtensions.FindPair(session.Board);
            session.Chain.Touch(a);
            session.Chain.Touch(b);

            MoveResult result = session.Commit();
            StepRef first = result.GetStep(result.GetPhase(0).Start);

            Assert.That(result.Is<SporeCleared>(first), Is.True);
            Assert.That(result.TryGet(first, out SporeFell _), Is.False);
        }

        sealed class StubRule : IMoveRule
        {
            public StubRule(MovePhase phase) => Phase = phase;
            public MovePhase Phase { get; }
            public void Apply(MoveContext context) { }
        }
    }
}
