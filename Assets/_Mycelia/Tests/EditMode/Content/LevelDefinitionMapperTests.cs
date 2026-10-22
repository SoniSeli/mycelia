using NUnit.Framework;
using Mycelia.Domain;
using Mycelia.Domain.Levels;

namespace Mycelia.Content.Tests
{
    public class LevelDefinitionMapperTests
    {
        ContentFactory content;
        SporeType circle;
        SporeType triangle;
        SporeType square;
        SporeCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            content = new ContentFactory();
            circle = content.Spore("spore-circle");
            triangle = content.Spore("spore-triangle");
            square = content.Spore("spore-square");
            catalog = content.Catalog(circle, triangle, square);
        }

        [TearDown]
        public void TearDown() => content.Dispose();

        LevelDefinition ValidLevel() =>
            content.Level("level-01", new[] { circle, triangle }, new[] { (circle, 10), (triangle, 5) });

        [Test]
        public void ToConfig_MapsAssetsToRuntimeIds()
        {
            LevelConfig config = LevelDefinitionMapper.ToConfig(ValidLevel(), catalog);

            Assert.That(config.Key, Is.EqualTo("level-01"));
            Assert.That(config.Width, Is.EqualTo(6));
            Assert.That(config.Moves, Is.EqualTo(20));
            Assert.That(config.SporeTypes, Is.EqualTo(new[] { new SporeTypeId(1), new SporeTypeId(2) }));
            Assert.That(config.Objectives[0].Type, Is.EqualTo(new SporeTypeId(1)));
            Assert.That(config.Objectives[1].Target, Is.EqualTo(5));
        }

        [Test]
        public void AValidLevel_HasNoProblems()
        {
            Assert.That(LevelDefinitionMapper.FindProblems(ValidLevel(), catalog), Is.Empty);
        }

        [Test]
        public void StarThresholds_DefaultToTheGlobalOnes_UnlessOverridden()
        {
            var level = ValidLevel();
            Assert.That(LevelDefinitionMapper.ToConfig(level, catalog).StarThresholds.TwoStarPercent, Is.EqualTo(25));

            level.SetStarThresholds(@override: true, twoStarPercent: 40, threeStarPercent: 70);
            var config = LevelDefinitionMapper.ToConfig(level, catalog);

            Assert.That(config.StarThresholds.TwoStarPercent, Is.EqualTo(40));
            Assert.That(config.StarThresholds.ThreeStarPercent, Is.EqualTo(70));
        }

        [Test]
        public void InvalidOverriddenStarThresholds_AreAProblem()
        {
            var level = ValidLevel();
            level.SetStarThresholds(@override: true, twoStarPercent: 60, threeStarPercent: 50);

            Assert.That(LevelDefinitionMapper.FindProblems(level, catalog), Has.Some.Contains("star"));
        }

        [Test]
        public void ASporeTypeMissingFromTheCatalog_IsAProblem()
        {
            var stranger = content.Spore("spore-stranger");
            var level = content.Level("level-01", new[] { circle, stranger }, new[] { (circle, 10) });

            Assert.That(LevelDefinitionMapper.FindProblems(level, catalog), Has.Some.Contains("spore-stranger").And.Contains("catalog"));
        }

        [Test]
        public void AnEmptySporeTypeEntry_IsAProblem()
        {
            var level = content.Level("level-01", new[] { circle, null }, new[] { (circle, 10) });

            Assert.That(LevelDefinitionMapper.FindProblems(level, catalog), Has.Some.Contains("empty"));
        }

        [Test]
        public void AnObjectiveWithNoSporeType_IsAProblem()
        {
            var level = content.Level("level-01", new[] { circle }, new[] { ((SporeType)null, 10) });

            Assert.That(LevelDefinitionMapper.FindProblems(level, catalog), Has.Some.Contains("Objective 1"));
        }

        [Test]
        public void DomainRules_AreReportedToo()
        {
            // Design §3.5: an objective for a type the level never spawns.
            var level = content.Level("level-01", new[] { circle }, new[] { (square, 10) });

            Assert.That(LevelDefinitionMapper.FindProblems(level, catalog), Has.Some.Contains("spore-square").And.Contains("not one of the level's spore types"));
        }

        [Test]
        public void ToConfig_ThrowsWithEveryProblem()
        {
            var level = content.Level("", new[] { circle }, new[] { (square, 10) }, moves: 0);

            var exception = Assert.Throws<InvalidLevelException>(() => LevelDefinitionMapper.ToConfig(level, catalog));

            Assert.That(exception.Problems.Count, Is.EqualTo(3));
        }

        [Test]
        public void AMissingCatalog_IsAProblem()
        {
            Assert.That(LevelDefinitionMapper.FindProblems(ValidLevel(), null), Has.Some.Contains("catalog"));
        }
    }
}
