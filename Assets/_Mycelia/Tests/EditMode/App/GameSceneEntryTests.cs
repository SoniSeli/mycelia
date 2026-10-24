using System;
using System.Reflection;
using Mycelia.Content;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using Mycelia.Game;
using NUnit.Framework;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Mycelia.App.Tests
{
    public class GameSceneEntryTests
    {
        const ulong Seed = 0x5EED;

        GameObject scene;
        BoardLayoutSettings layoutSettings;
        ChainInputSettings inputSettings;
        PlaybackSettings playbackSettings;
        GameSceneEntry entry;
        BoardView boardView;
        ResultPanel resultPanel;
        Button retry;
        Button resultLevelSelect;
        Button pauseLevelSelect;
        FakeProgressStore store;
        TMPro.TMP_Text factText;
        PauseOverlay pauseOverlay;
        Button resume;
        Button restart;
        PauseController pauseController;
        FakeSceneLoader loader;
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

            var line = new GameObject("Chain Line", typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(boardView.transform);
            var chainView = line.gameObject.AddComponent<ChainView>();
            SetReference(chainView, "line", line);
            var chainInput = new GameObject("Chain Input", typeof(ChainInputController)).GetComponent<ChainInputController>();
            chainInput.transform.SetParent(scene.transform);
            inputSettings = ScriptableObject.CreateInstance<ChainInputSettings>();
            SetReference(chainInput, "settings", inputSettings);

            SetReference(entry, "boardCamera", boardCamera);
            SetReference(entry, "boardView", boardView);
            SetReference(entry, "chainView", chainView);
            SetReference(entry, "chainInput", chainInput);
            var playback = boardView.gameObject.AddComponent<MovePlayback>();
            playbackSettings = ScriptableObject.CreateInstance<PlaybackSettings>();
            SetReference(playback, "settings", playbackSettings);
            SetReference(entry, "playback", playback);

            // HUD and result panel: just enough uGUI for the entry to wire them.
            Transform hudRoot = UiChild("HUD", scene.transform);
            var hud = hudRoot.gameObject.AddComponent<HudView>();
            var slotPrefab = UiChild("Slot", hudRoot).gameObject.AddComponent<ObjectiveSlot>();
            SetReference(slotPrefab, "icon", UiChild("Icon", slotPrefab.transform).gameObject.AddComponent<Image>());
            SetReference(slotPrefab, "count", UiChild("Count", slotPrefab.transform).gameObject.AddComponent<TextMeshProUGUI>());
            SetReference(hud, "band", hudRoot);
            SetReference(hud, "layout", layoutSettings);
            SetReference(hud, "movesText", UiChild("Moves", hudRoot).gameObject.AddComponent<TextMeshProUGUI>());
            SetReference(hud, "slotPrefab", slotPrefab);
            SetReference(hud, "slotsRoot", hudRoot);
            resultPanel = UiChild("Result Panel", hudRoot).gameObject.AddComponent<ResultPanel>();
            SetReference(resultPanel, "title", UiChild("Title", resultPanel.transform).gameObject.AddComponent<TextMeshProUGUI>());
            retry = UiChild("Retry", resultPanel.transform).gameObject.AddComponent<Button>();
            resultLevelSelect = UiChild("Level Select", resultPanel.transform).gameObject.AddComponent<Button>();
            Transform fact = UiChild("Fact", resultPanel.transform);
            factText = UiChild("Fact Text", fact).gameObject.AddComponent<TextMeshProUGUI>();
            SetReference(resultPanel, "levelSelect", resultLevelSelect);
            SetReference(resultPanel, "card", UiChild("Card", resultPanel.transform));
            SetReference(resultPanel, "fact", fact.gameObject);
            SetReference(resultPanel, "factImage", UiChild("Fact Image", fact).gameObject.AddComponent<Image>());
            SetReference(resultPanel, "factText", factText);
            SetReference(resultPanel, "factCredit", UiChild("Fact Credit", fact).gameObject.AddComponent<TextMeshProUGUI>());
            SetReference(resultPanel, "retry", retry);
            SetReference(hud, "resultPanel", resultPanel);
            SetReference(entry, "hud", hud);
            SetReference(entry, "resultPanel", resultPanel);

            pauseOverlay = UiChild("Pause Panel", scene.transform).gameObject.AddComponent<PauseOverlay>();
            resume = UiChild("Resume", pauseOverlay.transform).gameObject.AddComponent<Button>();
            restart = UiChild("Restart", pauseOverlay.transform).gameObject.AddComponent<Button>();
            pauseLevelSelect = UiChild("Level Select", pauseOverlay.transform).gameObject.AddComponent<Button>();
            SetReference(pauseOverlay, "resume", resume);
            SetReference(pauseOverlay, "restart", restart);
            SetReference(pauseOverlay, "levelSelect", pauseLevelSelect);
            pauseController = new GameObject("Pause", typeof(PauseController)).GetComponent<PauseController>();
            pauseController.transform.SetParent(scene.transform);
            SetReference(entry, "pauseOverlay", pauseOverlay);
            SetReference(entry, "pauseController", pauseController);

            seeds = new FixedSeedSource(Seed);
            loader = new FakeSceneLoader();
            store = new FakeProgressStore();
            services = new AppServices(loader, seeds, ProjectAssets.Spores, ProjectAssets.Levels, PlayerProgress.Load(store, ProjectAssets.Levels.Keys));
            level = ProjectAssets.TestLevel;
        }

        [TearDown]
        public void DestroyEntry()
        {
            Object.DestroyImmediate(scene);
            Object.DestroyImmediate(layoutSettings);
            Object.DestroyImmediate(inputSettings);
            Object.DestroyImmediate(playbackSettings);
        }

        static Transform UiChild(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        void SetDebugLevel(LevelDefinition debugLevel) => SetReference(entry, "debugLevel", debugLevel);

        // Calls the message Unity sends when the app goes to the background or comes back. SendMessage can't be used:
        // behaviours don't run in Edit Mode.
        void SendApplicationPause(bool paused) =>
            typeof(PauseController).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(pauseController, new object[] { paused });

        [Test]
        public void Retry_ReloadsTheGameScene_OnTheSameLevel()
        {
            entry.Construct(services, new GameSceneArgs(level));

            retry.onClick.Invoke();

            Assert.That(loader.Loads.Count, Is.EqualTo(1));
            Assert.That(loader.Loads[0].Scene, Is.EqualTo(SceneId.Game));
            Assert.That(((GameSceneArgs)loader.Loads[0].Args).Level, Is.SameAs(level));
        }

        [Test]
        public void Retry_AfterStartingFromTheDebugLevel_ReplaysThatLevel()
        {
            SetDebugLevel(level);
            entry.Construct(services, args: null);

            retry.onClick.Invoke();

            Assert.That(((GameSceneArgs)loader.Loads[0].Args).Level, Is.SameAs(level));
        }

        [Test]
        public void LevelSelect_OnTheResult_OpensLevelSelect()
        {
            entry.Construct(services, new GameSceneArgs(level));

            resultLevelSelect.onClick.Invoke();

            Assert.That(loader.Loads.Count, Is.EqualTo(1));
            Assert.That(loader.Loads[0].Scene, Is.EqualTo(SceneId.LevelSelect));
        }

        [Test]
        public void LevelSelect_OnThePauseOverlay_SavesFirst_ThenOpensLevelSelect()
        {
            // A win whose move was still playing when the player paused and left is recorded but not yet written.
            entry.Construct(services, new GameSceneArgs(level));
            services.Progress.RecordWin("level-01", 2);
            entry.Pause.Pause();

            pauseLevelSelect.onClick.Invoke();

            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(loader.Loads.Count, Is.EqualTo(1));
            Assert.That(loader.Loads[0].Scene, Is.EqualTo(SceneId.LevelSelect));
        }

        [Test]
        public void TheResult_CarriesTheLevelsFact()
        {
            entry.Construct(services, new GameSceneArgs(level));

            resultPanel.Show(Domain.Sessions.SessionState.Won, 1);

            Assert.That(factText.text, Is.EqualTo(level.FactText).And.Not.Empty);
        }

        [Test]
        public void GoingToTheBackground_PausesTheLevel_BehindTheOverlay()
        {
            entry.Construct(services, new GameSceneArgs(level));
            Assert.That(pauseOverlay.gameObject.activeSelf, Is.False, "Hidden while playing.");

            SendApplicationPause(true);

            Assert.That(entry.Pause.IsPaused, Is.True);
            Assert.That(pauseOverlay.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ComingBack_LeavesTheLevelPaused_UntilThePlayerResumes()
        {
            entry.Construct(services, new GameSceneArgs(level));
            SendApplicationPause(true);

            SendApplicationPause(false);
            Assert.That(entry.Pause.IsPaused, Is.True, "The level waits for the player.");

            resume.onClick.Invoke();
            Assert.That(entry.Pause.IsPaused, Is.False);
            Assert.That(pauseOverlay.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Restart_ReloadsTheGameScene_OnTheSameLevel()
        {
            entry.Construct(services, new GameSceneArgs(level));
            entry.Pause.Pause();

            restart.onClick.Invoke();

            Assert.That(loader.Loads.Count, Is.EqualTo(1));
            Assert.That(loader.Loads[0].Scene, Is.EqualTo(SceneId.Game));
            Assert.That(((GameSceneArgs)loader.Loads[0].Args).Level, Is.SameAs(level));
        }

        [Test]
        public void WhilePaused_TheGameIdlesAtTheMenuFrameRate()
        {
            int before = Application.targetFrameRate;
            try
            {
                entry.Construct(services, new GameSceneArgs(level));

                entry.Pause.Pause();
                Assert.That(Application.targetFrameRate, Is.EqualTo(FrameRatePolicy.Menus));

                entry.Pause.Resume();
                Assert.That(Application.targetFrameRate, Is.EqualTo(FrameRatePolicy.Gameplay));
            }
            finally
            {
                Application.targetFrameRate = before;
            }
        }

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
