using NUnit.Framework;

namespace Mycelia.Content.Tests
{
    public class LevelCatalogTests
    {
        ContentFactory content;
        SporeType circle;
        SporeType triangle;
        SporeCatalog spores;

        [SetUp]
        public void SetUp()
        {
            content = new ContentFactory();
            circle = content.Spore("spore-circle");
            triangle = content.Spore("spore-triangle");
            spores = content.Catalog(circle, triangle);
        }

        [TearDown]
        public void TearDown() => content.Dispose();

        LevelDefinition Level(string key) =>
            content.Level(key, new[] { circle, triangle }, new[] { (circle, 10) });

        [Test]
        public void AValidCatalog_HasNoProblems()
        {
            var catalog = content.LevelCatalog(Level("level-01"), Level("level-02"));

            Assert.That(catalog.FindProblems(spores), Is.Empty);
        }

        [Test]
        public void Keys_AreInPlayOrder()
        {
            var catalog = content.LevelCatalog(Level("level-02"), Level("level-01"));

            Assert.That(catalog.Keys, Is.EqualTo(new[] { "level-02", "level-01" }));
        }

        [Test]
        public void TryGetByKey_FindsTheLevel()
        {
            var second = Level("level-02");
            var catalog = content.LevelCatalog(Level("level-01"), second);

            Assert.That(catalog.TryGetByKey("level-02", out LevelDefinition found), Is.True);
            Assert.That(found, Is.SameAs(second));
            Assert.That(catalog.TryGetByKey("level-03", out _), Is.False);
        }

        [Test]
        public void AnEmptyCatalog_IsAProblem()
        {
            var catalog = content.LevelCatalog();

            Assert.That(catalog.FindProblems(spores), Has.Some.Contains("no levels"));
        }

        [Test]
        public void AnEmptyEntry_IsAProblem()
        {
            var catalog = content.LevelCatalog(Level("level-01"), null);

            Assert.That(catalog.FindProblems(spores), Has.Some.Contains("empty"));
        }

        [Test]
        public void TheSameLevelListedTwice_IsAProblem()
        {
            var level = Level("level-01");
            var catalog = content.LevelCatalog(level, level);

            Assert.That(catalog.FindProblems(spores), Has.Some.Contains("listed more than once"));
        }

        // Saves and analytics name levels by key (ADR-0006), so every catalog key reads the same: level-NN.
        [TestCase("")]
        [TestCase("level-1")]
        [TestCase("level-001")]
        [TestCase("Level-01")]
        [TestCase("level-test")]
        public void AKeyThatIsNotLevelAndTwoDigits_IsAProblem(string key)
        {
            var catalog = content.LevelCatalog(Level(key));

            Assert.That(catalog.FindProblems(spores), Has.Some.Contains("level-NN"));
        }

        [Test]
        public void ADuplicateKey_IsAProblem()
        {
            var catalog = content.LevelCatalog(Level("level-01"), Level("level-01"));

            Assert.That(catalog.FindProblems(spores), Has.Some.Contains("more than one level"));
        }

        [Test]
        public void AnUnplayableLevel_IsAProblem_NamedByItsAsset()
        {
            var unplayable = content.Level("level-01", new[] { circle, triangle }, new[] { (circle, 10) }, moves: 0);
            unplayable.name = "broken-level";
            var catalog = content.LevelCatalog(unplayable);

            Assert.That(catalog.FindProblems(spores), Has.Some.StartsWith("'broken-level':"));
        }

        [Test]
        public void NoSporeCatalog_IsAProblem()
        {
            var catalog = content.LevelCatalog(Level("level-01"));

            Assert.That(catalog.FindProblems(null), Has.Some.Contains("spore catalog"));
        }
    }
}
