using System;
using Mycelia.Content;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mycelia.App.Tests
{
    public class GameSceneEntryTests
    {
        const ulong Seed = 0x5EED;

        GameObject gameObject;
        GameSceneEntry entry;
        FixedSeedSource seeds;
        AppServices services;
        LevelDefinition level;

        [SetUp]
        public void CreateEntry()
        {
            gameObject = new GameObject("Game");
            entry = gameObject.AddComponent<GameSceneEntry>();
            seeds = new FixedSeedSource(Seed);
            services = new AppServices(new FakeSceneLoader(), seeds, ProjectAssets.Spores);
            level = ProjectAssets.TestLevel;
        }

        [TearDown]
        public void DestroyEntry() => Object.DestroyImmediate(gameObject);

        void SetDebugLevel(LevelDefinition debugLevel)
        {
            var serialized = new SerializedObject(entry);
            serialized.FindProperty("debugLevel").objectReferenceValue = debugLevel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Construct_StartsASessionOnTheLevelInTheArgs()
        {
            entry.Construct(services, new GameSceneArgs(level));

            Assert.That(entry.Session, Is.Not.Null);
            Assert.That(entry.Session.Config.Key, Is.EqualTo(level.Key));
            Assert.That(entry.Session.State, Is.EqualTo(Domain.Sessions.SessionState.Playing));
        }

        [Test]
        public void EachAttempt_GetsItsBoardFromAFreshSeed()
        {
            entry.Construct(services, new GameSceneArgs(level));

            var expected = new LevelSession(LevelDefinitionMapper.ToConfig(level, services.Spores), new Pcg32(Seed)).Board;
            Assert.That(seeds.Draws, Is.EqualTo(1));
            for (int column = 0; column < expected.Width; column++)
            for (int row = 0; row < expected.Height; row++)
            {
                var cell = new GridPosition(column, row);
                Assert.That(entry.Session.Board.GetOccupant(cell), Is.EqualTo(expected.GetOccupant(cell)), cell.ToString());
            }
        }

        [Test]
        public void WithoutArgs_TheDebugLevelIsPlayed()
        {
            SetDebugLevel(level);

            entry.Construct(services, args: null);

            Assert.That(entry.Session.Config.Key, Is.EqualTo(level.Key));
        }

        [Test]
        public void WithoutArgsOrADebugLevel_TheSceneCannotStart()
        {
            Assert.Throws<InvalidOperationException>(() => entry.Construct(services, args: null));
        }
    }
}
