using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Tests.Levels
{
    public class LevelConfigTests
    {
        static readonly SporeTypeId A = Boards.IdOf('A');
        static readonly SporeTypeId B = Boards.IdOf('B');
        static readonly SporeTypeId C = Boards.IdOf('C');

        static IReadOnlyList<string> Problems(
            string key = "level-01",
            int width = 6,
            int height = 6,
            int moves = 20,
            SporeTypeId[] types = null,
            ObjectiveDefinition[] objectives = null) =>
            LevelConfig.FindProblems(
                key, width, height, moves,
                types ?? new[] { A, B },
                objectives ?? new[] { new ObjectiveDefinition(A, 5) });

        [Test]
        public void AValidLevel_HasNoProblems()
        {
            Assert.That(Problems(), Is.Empty);
        }

        [Test]
        public void Constructor_KeepsTheValues()
        {
            var config = new LevelConfig("level-07", 6, 5, 18, new[] { A, B }, new[] { new ObjectiveDefinition(B, 9) },
                new StarThresholds(30, 60));

            Assert.That(config.Key, Is.EqualTo("level-07"));
            Assert.That(config.Width, Is.EqualTo(6));
            Assert.That(config.Height, Is.EqualTo(5));
            Assert.That(config.Moves, Is.EqualTo(18));
            Assert.That(config.SporeTypes, Is.EqualTo(new[] { A, B }));
            Assert.That(config.StarThresholds.TwoStarPercent, Is.EqualTo(30));
        }

        [Test]
        public void StarThresholds_DefaultToTheGlobalOnes()
        {
            var config = new LevelConfig("level-01", 6, 6, 20, new[] { A }, new[] { new ObjectiveDefinition(A, 5) });

            Assert.That(config.StarThresholds.TwoStarPercent, Is.EqualTo(StarThresholds.Default.TwoStarPercent));
            Assert.That(config.StarThresholds.ThreeStarPercent, Is.EqualTo(StarThresholds.Default.ThreeStarPercent));
        }

        [Test]
        public void AnObjectiveForATypeNotInTheLevel_IsAProblem()
        {
            // Design §3.5 / ADR-0005: the prototype could ask for spores it never spawned.
            var problems = Problems(objectives: new[] { new ObjectiveDefinition(C, 5) });

            Assert.That(problems, Has.Some.Contains("not one of the level's spore types"));
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase(null)]
        public void AMissingKey_IsAProblem(string key)
        {
            Assert.That(Problems(key: key), Has.Some.Contains("key"));
        }

        [TestCase(0, 6)]
        [TestCase(6, 0)]
        [TestCase(1, 1)]
        public void ABoardWithoutRoomForAChain_IsAProblem(int width, int height)
        {
            Assert.That(Problems(width: width, height: height), Has.Some.Contains("board"));
        }

        [Test]
        public void NoMoves_IsAProblem()
        {
            Assert.That(Problems(moves: 0), Has.Some.Contains("at least one move"));
        }

        [Test]
        public void NoSporeTypes_IsAProblem()
        {
            Assert.That(Problems(types: new SporeTypeId[0]), Has.Some.Contains("spore type"));
        }

        [Test]
        public void ADuplicateSporeType_IsAProblem()
        {
            Assert.That(Problems(types: new[] { A, B, A }), Has.Some.Contains("more than once"));
        }

        [Test]
        public void TheEmptySporeType_IsAProblem()
        {
            Assert.That(Problems(types: new[] { A, SporeTypeId.None }), Has.Some.Contains("empty"));
        }

        [Test]
        public void NoObjectives_IsAProblem()
        {
            Assert.That(Problems(objectives: new ObjectiveDefinition[0]), Has.Some.Contains("objective"));
        }

        [Test]
        public void ANonPositiveObjectiveTarget_IsAProblem()
        {
            Assert.That(Problems(objectives: new[] { new ObjectiveDefinition(A, 0) }), Has.Some.Contains("target"));
        }

        [Test]
        public void EveryProblem_IsReportedTogether()
        {
            var problems = Problems(key: "", moves: 0, objectives: new[] { new ObjectiveDefinition(C, 5) });

            Assert.That(problems.Count, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_ThrowsWithEveryProblem()
        {
            var exception = Assert.Throws<InvalidLevelException>(() =>
                new LevelConfig("", 6, 6, 0, new[] { A }, new[] { new ObjectiveDefinition(A, 5) }));

            Assert.That(exception.Problems.Count, Is.EqualTo(2));
        }

        [Test]
        public void CreateObjectives_ReturnsFreshObjectivesEachTime()
        {
            var config = new LevelConfig("level-01", 6, 6, 20, new[] { A, B },
                new[] { new ObjectiveDefinition(A, 5), new ObjectiveDefinition(B, 3) });

            var first = config.CreateObjectives();
            var second = config.CreateObjectives();

            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(first.Select(o => o.Target), Is.EqualTo(new[] { 5, 3 }));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
        }
    }
}
