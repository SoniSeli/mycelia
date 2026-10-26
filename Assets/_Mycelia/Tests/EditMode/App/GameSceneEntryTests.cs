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
        Button pauseQuit;
        Button hudPause;
        TMP_Text hudTitle;
        Button hudSettings;
        TestSettingsModal settings;
        FakeProgressStore store;
        FakeAnalytics analytics;
        FakeHaptics haptics;
        FakeSounds sounds;
        ChainInputController chainInput;
        TMPro.TMP_Text factText;
        PauseOverlay pauseOverlay;
        Button resume;
        Button restart;
        PauseController pauseController;
        ParticleSystem spores;
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
            boardView = new GameObject("Board", typeof(BoardView)).GetComponent<BoardView>();
            boardView.transform.SetParent(scene.transform);
            SetReference(boardView, "sporePrefab", sporeView);

            var line = new GameObject("Chain Line", typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(boardView.transform);
            var chainView = line.gameObject.AddComponent<ChainView>();
            SetReference(chainView, "line", line);
            chainInput = new GameObject("Chain Input", typeof(ChainInputController)).GetComponent<ChainInputController>();
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
            hudPause = UiChild("Pause Button", hudRoot).gameObject.AddComponent<Button>();
            SetReference(hud, "pauseButton", hudPause);
            hudTitle = UiChild("Title", hudRoot).gameObject.AddComponent<TextMeshProUGUI>();
            SetReference(hud, "title", hudTitle);
            hudSettings = UiChild("Settings Button", hudRoot).gameObject.AddComponent<Button>();
            SetReference(hud, "settingsButton", hudSettings);
            resultPanel = UiChild("Result Panel", hudRoot).gameObject.AddComponent<ResultPanel>();
            SetReference(resultPanel, "title", UiChild("Title", resultPanel.transform).gameObject.AddComponent<TextMeshProUGUI>());
            retry = UiChild("Retry", resultPanel.transform).gameObject.AddComponent<Button>();
            resultLevelSelect = UiChild("Level Select", resultPanel.transform).gameObject.AddComponent<Button>();
            Transform fact = UiChild("Fact", resultPanel.transform);
            factText = UiChild("Fact Text", fact).gameObject.AddComponent<TextMeshProUGUI>();
            SetReference(resultPanel, "levelSelect", resultLevelSelect);
            var resultCard = (RectTransform)UiChild("Card", resultPanel.transform);
            SetReference(resultPanel, "card", resultCard);
            SetReference(resultPanel, "cardBackground", resultCard.gameObject.AddComponent<Image>());
            SetReference(resultPanel, "dim", UiChild("Dim", resultPanel.transform).gameObject);
            SetReference(resultPanel, "fact", fact.gameObject);
            SetReference(resultPanel, "factHeading", UiChild("Interesting Fact", fact).gameObject.AddComponent<TextMeshProUGUI>());
            // The panel lays the fact out under its stars.
            var serializedPanel = new SerializedObject(resultPanel);
            SerializedProperty resultStars = serializedPanel.FindProperty("stars");
            resultStars.arraySize = 1;
            resultStars.GetArrayElementAtIndex(0).objectReferenceValue = UiChild("Star 1", resultPanel.transform).gameObject.AddComponent<Image>();
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            SetReference(resultPanel, "factImage", UiChild("Fact Image", fact).gameObject.AddComponent<Image>());
            SetReference(resultPanel, "factText", factText);
            SetReference(resultPanel, "factCredit", UiChild("Fact Credit", fact).gameObject.AddComponent<TextMeshProUGUI>());
            SetReference(resultPanel, "retry", retry);
            SetReference(hud, "resultPanel", resultPanel);
            SetReference(entry, "hud", hud);
            var winWave = scene.AddComponent<WinWave>();
            SetReference(winWave, "skip", UiChild("Skip", hudRoot).gameObject.AddComponent<Button>());
            SetReference(entry, "winWave", winWave);
            SetReference(entry, "resultPanel", resultPanel);

            pauseOverlay = UiChild("Pause Panel", scene.transform).gameObject.AddComponent<PauseOverlay>();
            resume = UiChild("Resume", pauseOverlay.transform).gameObject.AddComponent<Button>();
            restart = UiChild("Restart", pauseOverlay.transform).gameObject.AddComponent<Button>();
            pauseQuit = UiChild("Quit", pauseOverlay.transform).gameObject.AddComponent<Button>();
            SetReference(pauseOverlay, "resume", resume);
            SetReference(pauseOverlay, "restart", restart);
            SetReference(pauseOverlay, "quit", pauseQuit);
            pauseController = new GameObject("Pause", typeof(PauseController)).GetComponent<PauseController>();
            pauseController.transform.SetParent(scene.transform);
            SetReference(entry, "pauseOverlay", pauseOverlay);
            SetReference(entry, "pauseController", pauseController);
            settings = new TestSettingsModal(scene.transform);
            SetReference(entry, "settings", settings.Modal);

            var backdropObject = new GameObject("Backdrop", typeof(MeshFilter), typeof(MeshRenderer));
            backdropObject.transform.SetParent(camera.transform, false);
            spores = new GameObject("Spores").AddComponent<ParticleSystem>();
            spores.transform.SetParent(backdropObject.transform, false);
            var backdrop = backdropObject.AddComponent<Backdrop>();
            SetReference(backdrop, "viewCamera", camera);
            SetReference(backdrop, "gradient", backdropObject.GetComponent<MeshFilter>());
            SetReference(backdrop, "spores", spores);
            SetReference(entry, "backdrop", backdrop);

            seeds = new FixedSeedSource(Seed);
            loader = new FakeSceneLoader();
            store = new FakeProgressStore();
            analytics = new FakeAnalytics();
            haptics = new FakeHaptics();
            sounds = new FakeSounds();
            services = new AppServices(loader, seeds, ProjectAssets.Spores, ProjectAssets.Levels, PlayerProgress.Load(store, ProjectAssets.Levels.Keys), analytics, haptics, sounds);
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
        public void Quit_OnThePauseOverlay_SavesFirst_ThenOpensLevelSelect()
        {
            // A win whose move was still playing when the player paused and left is recorded but not yet written.
            entry.Construct(services, new GameSceneArgs(level));
            services.Progress.RecordWin("level-01", 2);
            entry.Pause.Pause();

            pauseQuit.onClick.Invoke();

            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(loader.Loads.Count, Is.EqualTo(1));
            Assert.That(loader.Loads[0].Scene, Is.EqualTo(SceneId.LevelSelect));
        }

        [Test]
        public void Construct_LogsLevelStart_WithTheKeyAndTheAttemptsSeed()
        {
            entry.Construct(services, new GameSceneArgs(level));

            AnalyticsEvent start = analytics.Single("level_start");
            Assert.That(start.Value("level"), Is.EqualTo(level.Key));
            Assert.That(start.Value("seed"), Is.EqualTo(Seed.ToString("x16")));
        }

        // ADR-0013: the background spores stop under the pause overlay with everything else.
        [Test]
        public void PausingTheLevel_FreezesTheBackdropsSpores_AndResumingLetsThemDrift()
        {
            entry.Construct(services, new GameSceneArgs(level));
            spores.Play();

            entry.Pause.Pause();
            Assert.That(spores.isPaused, Is.True);

            entry.Pause.Resume();
            Assert.That(spores.isPlaying, Is.True);
        }

        [Test]
        public void RestartingFromThePauseOverlay_LogsTheAttemptAsAbandoned()
        {
            entry.Construct(services, new GameSceneArgs(level));
            entry.Pause.Pause();

            restart.onClick.Invoke();

            Assert.That(analytics.Single("level_end").Value("outcome"), Is.EqualTo("abandoned"));
        }

        [Test]
        public void LeavingFromThePauseOverlay_LogsTheAttemptAsAbandoned()
        {
            entry.Construct(services, new GameSceneArgs(level));
            entry.Pause.Pause();

            pauseQuit.onClick.Invoke();

            Assert.That(analytics.Single("level_end").Value("outcome"), Is.EqualTo("abandoned"));
        }

        [Test]
        public void TheResult_CarriesTheLevelsFact()
        {
            entry.Construct(services, new GameSceneArgs(level));

            resultPanel.Show(Domain.Sessions.SessionState.Won, 1);

            Assert.That(factText.text, Is.EqualTo(level.FactText).And.Not.Empty);
        }

        // The HUD names the level by its place in the catalog, the number level select shows.
        [Test]
        public void TheHudTitle_NumbersTheLevelByItsPlaceInTheCatalog()
        {
            entry.Construct(services, new GameSceneArgs(ProjectAssets.Levels.Levels[8]));

            Assert.That(hudTitle.text, Is.EqualTo("Level 9"));
        }

        // Decided 2026-10-25 (slice 5): settings opened mid-level pause it and cover it alone; closing them resumes play.
        [Test]
        public void TheSettingsButton_PausesTheLevel_ShowingSettingsNotThePauseModal()
        {
            entry.Construct(services, new GameSceneArgs(level));

            hudSettings.onClick.Invoke();

            Assert.That(entry.Pause.IsPaused, Is.True);
            Assert.That(settings.Modal.IsOpen, Is.True);
            Assert.That(pauseOverlay.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ClosingSettings_ResumesPlay()
        {
            entry.Construct(services, new GameSceneArgs(level));
            hudSettings.onClick.Invoke();

            settings.Close.onClick.Invoke();

            Assert.That(entry.Pause.IsPaused, Is.False);
            Assert.That(settings.Modal.IsOpen, Is.False);
        }

        [Test]
        public void Back_WithSettingsOpen_ResumesPlay()
        {
            entry.Construct(services, new GameSceneArgs(level));
            hudSettings.onClick.Invoke();

            pauseController.Back();

            Assert.That(entry.Pause.IsPaused, Is.False);
            Assert.That(settings.Modal.IsOpen, Is.False);
            Assert.That(loader.Loads, Is.Empty);
        }

        // Design §8.3: a pause button in the HUD opens the same pause as back and going to the background.
        [Test]
        public void ThePauseButton_PausesTheLevel_BehindTheOverlay()
        {
            entry.Construct(services, new GameSceneArgs(level));

            hudPause.onClick.Invoke();

            Assert.That(entry.Pause.IsPaused, Is.True);
            Assert.That(pauseOverlay.gameObject.activeSelf, Is.True);
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

        static bool InChain(LevelSession session, GridPosition cell)
        {
            foreach (GridPosition chained in session.Chain.Cells)
                if (chained.Equals(cell)) return true;
            return false;
        }

        // Three cells the session accepts as a chain, found by trying them; the chain is left empty again.
        static GridPosition[] FindChainOfThree(LevelSession session)
        {
            IReadOnlyBoard board = session.Board;
            for (int column = 0; column < board.Width; column++)
            for (int row = 0; row < board.Height; row++)
            {
                session.Chain.Touch(new GridPosition(column, row));
                for (int step = 0; step < 2; step++)
                {
                    GridPosition last = session.Chain.Cells[session.Chain.Count - 1];
                    for (int dc = -1; dc <= 1 && session.Chain.Count == step + 1; dc++)
                    for (int dr = -1; dr <= 1 && session.Chain.Count == step + 1; dr++)
                    {
                        var next = new GridPosition(last.Column + dc, last.Row + dr);
                        if (next.Column < 0 || next.Row < 0 || next.Column >= board.Width || next.Row >= board.Height || InChain(session, next))
                            continue;
                        session.Chain.Touch(next);
                    }
                }
                if (session.Chain.Count == 3)
                {
                    GridPosition[] cells = { session.Chain.Cells[0], session.Chain.Cells[1], session.Chain.Cells[2] };
                    session.Chain.Clear();
                    return cells;
                }
                session.Chain.Clear();
            }
            throw new InvalidOperationException("The test level's board has no chain of three.");
        }

        [Test]
        public void DraggingAChain_TicksPerSpore_AndCommittingItPlaysTheCommitHaptic()
        {
            entry.Construct(services, new GameSceneArgs(level));
            GridPosition[] cells = FindChainOfThree(entry.Session);
            var drag = (ChainDrag)typeof(ChainInputController).GetField("drag", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(chainInput);

            drag.Press(new Vector2(cells[0].Column + 0.5f, cells[0].Row + 0.5f));
            drag.Move(new Vector2(cells[1].Column + 0.5f, cells[1].Row + 0.5f));
            drag.Move(new Vector2(cells[2].Column + 0.5f, cells[2].Row + 0.5f));
            Assert.That(drag.Release(), Is.Not.Null);

            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Tick, Haptic.Tick, Haptic.Tick, Haptic.Commit }));
        }

        // Playback can't run its sequence in Edit Mode, so the test tells its listener what it would at the end of a move.
        [Test]
        public void TheOutcome_IsFelt_WhenPlaybackReportsIt()
        {
            entry.Construct(services, new GameSceneArgs(level));
            var listener = (IPlaybackListener)typeof(MovePlayback).GetField("listener", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(boardView.GetComponent<MovePlayback>());

            listener.SessionEnded(Domain.Sessions.SessionState.Lost, 0);

            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Loss }));
        }

        // ADR-0017: the chain climbs a scale as it grows, and pops when committed.
        [Test]
        public void DraggingAChain_TicksAStepHigherPerSpore_AndCommittingItPops()
        {
            entry.Construct(services, new GameSceneArgs(level));
            GridPosition[] cells = FindChainOfThree(entry.Session);
            var drag = (ChainDrag)typeof(ChainInputController).GetField("drag", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(chainInput);

            drag.Press(new Vector2(cells[0].Column + 0.5f, cells[0].Row + 0.5f));
            drag.Move(new Vector2(cells[1].Column + 0.5f, cells[1].Row + 0.5f));
            drag.Move(new Vector2(cells[2].Column + 0.5f, cells[2].Row + 0.5f));
            Assert.That(drag.Release(), Is.Not.Null);

            Assert.That(sounds.Ticks, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(sounds.Played, Is.EqualTo(new[] { SoundEffect.Tick, SoundEffect.Tick, SoundEffect.Tick, SoundEffect.Commit }));
            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Tick, Haptic.Tick, Haptic.Tick, Haptic.Commit }), "Still felt too.");
        }

        [Test]
        public void TheOutcome_IsHeard_WhenPlaybackReportsIt()
        {
            entry.Construct(services, new GameSceneArgs(level));
            var listener = (IPlaybackListener)typeof(MovePlayback).GetField("listener", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(boardView.GetComponent<MovePlayback>());

            listener.SessionEnded(Domain.Sessions.SessionState.Won, 2);

            Assert.That(sounds.Played, Is.EqualTo(new[] { SoundEffect.Win }));
            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Win }), "Still felt too.");
        }

        // ADR-0013: nothing of the level keeps sounding under the pause overlay; its buttons still tap.
        [Test]
        public void PausingTheLevel_PausesItsSounds_AndThePauseButtonTaps()
        {
            entry.Construct(services, new GameSceneArgs(level));

            hudPause.onClick.Invoke();
            resume.onClick.Invoke();

            Assert.That(sounds.Paused, Is.EqualTo(new[] { true, false }));
            Assert.That(sounds.Played, Is.EqualTo(new[] { SoundEffect.Tap, SoundEffect.Tap }));
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
