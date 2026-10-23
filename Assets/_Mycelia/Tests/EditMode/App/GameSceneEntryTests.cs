using System;
using Mycelia.Content;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using Mycelia.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mycelia.App.Tests
{
    public class GameSceneEntryTests
    {
        const ulong Seed = 0x5EED;

        GameObject scene;
        BoardLayoutSettings layoutSettings;
        GameSceneEntry entry;
        BoardView boardView;
        FixedSeedSource seeds;
        AppServices services;
        LevelDefinition level;

        // A stand-in for the Game scene: the entry, a camera and a board view, with scene objects as prefabs.
        [SetUp]
        public void CreateEntry()
        {
            scene = new GameObject("Game");
            entry = scene.AddComponent<GameSceneEntry>();

            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(scene.transform);
            camera.orthographic = true;
            var boardCamera = camera.gameObject.AddComponent<BoardCamera>();
            layoutSettings = ScriptableObject.CreateInstance<BoardLayoutSettings>();
            SetReference(boardCamera, "boardCamera", camera);
            SetReference(boardCamera, "settings", layoutSettings);

            var sporePrefab = new GameObject("Spore Prefab", typeof(SpriteRenderer));
            sporePrefab.transform.SetParent(scene.transform);
            var sporeView = sporePrefab.AddComponent<SporeView>();
            SetReference(sporeView, "spriteRenderer", sporePrefab.GetComponent<SpriteRenderer>());
            var cellPrefab = new GameObject("Cell Prefab", typeof(SpriteRenderer));
            cellPrefab.transform.SetParent(scene.transform);
            boardView = new GameObject("Board", typeof(BoardView)).GetComponent<BoardView>();
            boardView.transform.SetParent(scene.transform);
            SetReference(boardView, "sporePrefab", sporeView);
            SetReference(boardView, "cellPrefab", cellPrefab.GetComponent<SpriteRenderer>());

            SetReference(entry, "boardCamera", boardCamera);
            SetReference(entry, "boardView", boardView);

            seeds = new FixedSeedSource(Seed);
            services = new AppServices(new FakeSceneLoader(), seeds, ProjectAssets.Spores);
            level = ProjectAssets.TestLevel;
        }

        [TearDown]
        public void DestroyEntry()
        {
            Object.DestroyImmediate(scene);
            Object.DestroyImmediate(layoutSettings);
        }

        static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        void SetDebugLevel(LevelDefinition debugLevel) => SetReference(entry, "debugLevel", debugLevel);

        [Test]
        public void Construct_ShowsTheStartingBoard()
        {
            entry.Construct(services, new GameSceneArgs(level));

            IReadOnlyBoard board = entry.Session.Board;
            for (int column = 0; column < board.Width; column++)
            for (int row = 0; row < board.Height; row++)
            {
                var cell = new GridPosition(column, row);
                SporeView spore = boardView.SporeAt(cell);
                Assert.That(spore, Is.Not.Null, cell.ToString());
                Assert.That(spore.Renderer.sprite, Is.SameAs(services.Spores.TypeOf(board.GetOccupant(cell)).Sprite), cell.ToString());
            }
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
