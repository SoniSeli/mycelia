using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Mycelia.Content.Tests
{
    /// <summary>Guards the real assets in the project, so a broken level or catalog fails CI, not a player's game.</summary>
    public class ProjectContentTests
    {
        static T[] LoadAll<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

        [Test]
        public void TheProjectHasExactlyOneValidSporeCatalog()
        {
            var catalogs = LoadAll<SporeCatalog>();

            Assert.That(catalogs.Length, Is.EqualTo(1), "Exactly one SporeCatalog asset is expected.");
            Assert.That(catalogs[0].FindProblems(), Is.Empty);
        }

        [Test]
        public void EveryLevelDefinitionInTheProject_IsPlayable()
        {
            var catalog = LoadAll<SporeCatalog>().Single();
            var levels = LoadAll<LevelDefinition>();

            Assert.That(levels, Is.Not.Empty, "The project should contain at least one level.");
            foreach (LevelDefinition level in levels)
            {
                Assert.That(LevelDefinitionMapper.FindProblems(level, catalog), Is.Empty, $"{level.name} has problems.");
            }
        }

        // Saves and analytics name levels by key (ADR-0006): renaming or removing a shipped key would lose players'
        // stars and unlocks. Add a key here once its level ships; never remove one.
        static readonly string[] ShippedLevelKeys = { "level-01", "level-02", "level-03" };

        [Test]
        public void TheProjectHasExactlyOneValidLevelCatalog()
        {
            var catalogs = LoadAll<LevelCatalog>();

            Assert.That(catalogs.Length, Is.EqualTo(1), "Exactly one LevelCatalog asset is expected.");
            Assert.That(catalogs[0].FindProblems(LoadAll<SporeCatalog>().Single()), Is.Empty);
        }

        [Test]
        public void EveryShippedLevelKey_IsStillInTheCatalog()
        {
            var keys = LoadAll<LevelCatalog>().Single().Keys;

            Assert.That(ShippedLevelKeys, Is.SubsetOf(keys));
        }

        [Test]
        public void TheTestLevel_StaysOutOfTheCatalog()
        {
            // level-test is the Game scene's Debug Level and a test fixture; players never see it.
            var keys = LoadAll<LevelCatalog>().Single().Keys;

            Assert.That(keys, Has.No.Member("level-test"));
        }

        [Test]
        public void LevelKeys_AreUnique()
        {
            var keys = LoadAll<LevelDefinition>().Select(level => level.Key).ToArray();

            Assert.That(keys, Is.Unique);
        }
    }
}
