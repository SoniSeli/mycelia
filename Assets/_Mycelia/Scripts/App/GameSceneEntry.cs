using System;
using Mycelia.Content;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The Game scene's entry point: starts a <see cref="LevelSession"/> on the level it is given, with a fresh seed.
    /// It fits the camera to the level, shows the starting board and the HUD, connects chain input to move playback,
    /// which reports to the HUD, and sets up pausing.
    /// </summary>
    public sealed class GameSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField, Tooltip("The level to play when Play is pressed in this scene in the Editor (ADR-0010).")]
        LevelDefinition debugLevel;

        [SerializeField] BoardCamera boardCamera;
        [SerializeField] BoardView boardView;
        [SerializeField] ChainView chainView;
        [SerializeField] ChainInputController chainInput;
        [SerializeField] MovePlayback playback;
        [SerializeField] HudView hud;
        [SerializeField] WinWave winWave;
        [SerializeField] ResultPanel resultPanel;
        [SerializeField] PauseOverlay pauseOverlay;
        [SerializeField] PauseController pauseController;
        [SerializeField] SettingsModal settings;
        [SerializeField] Backdrop backdrop;

        public LevelSession Session { get; private set; }
        public LevelPause Pause { get; private set; }

        public void Construct(AppServices services, SceneArgs args)
        {
            LevelDefinition level = args is GameSceneArgs game ? game.Level : debugLevel;
            if (level == null)
                throw new InvalidOperationException("The Game scene needs a level: open it with GameSceneArgs, or set its Debug Level.");

            LevelConfig config = LevelDefinitionMapper.ToConfig(level, services.Spores);
            ulong seed = services.Seeds.Next();
            if (Debug.isDebugBuild)
                Debug.Log($"[Mycelia] Level '{config.Key}' starts with seed {seed:x16}.");

            Session = new LevelSession(config, new Pcg32(seed));
            services.Analytics.Log(new AnalyticsEvent("level_start").With("level", config.Key).With("seed", seed.ToString("x16")));
            boardCamera.Init(config.Width, config.Height);
            boardView.Init(Session.Board, services.Spores);
            chainView.Init(boardView, config.Width * config.Height);
            // Settings opened mid-level pause it and cover it alone; closing them resumes play (design §8.3 slice 5).
            var screen = new PauseScreen(pauseOverlay, settings, () => SettingsSwitches.ToChoice(services.Progress.Settings));
            Pause = new LevelPause(Session, playback, playback, screen, hud, backdrop, new PausedFrameRate(SceneId.Game));
            settings.Init(choice => SettingsSwitches.Choose(services.Progress, choice), () => Pause.Resume());
            Action openSettings = () =>
            {
                if (Pause.CanPause)
                {
                    screen.ShowSettingsOnPause();
                    Pause.Pause();
                }
                else
                {
                    // The level is over: nothing to pause.
                    settings.Open(SettingsSwitches.ToChoice(services.Progress.Settings));
                }
            };
            // Numbered by place in the catalog, as on level select; the Editor's debug level has none (0).
            int number = 0;
            for (int i = 0; i < services.Levels.Levels.Count && number == 0; i++)
                if (services.Levels.Levels[i] == level)
                    number = i + 1;
            hud.Init(config, number, services.Spores, Pause.Pause, openSettings);
            // A win plays its wave on the board before the HUD shows the result (Soni, 2026-10-25).
            winWave.Init(boardView, config.Width, config.Height, () => playback.LastCleared, hud);
            // Wins are recorded as they are committed and saved before the result shows (ADR-0006).
            var results = new LevelResultRecorder(Session, services.Progress, playback, winWave, services.Analytics);
            // Retry and Restart start the same level with a fresh seed, through the loader like any scene change.
            Action restart = () =>
            {
                results.Leave();
                services.Scenes.Load(SceneId.Game, services, new GameSceneArgs(level));
            };
            // Leaving writes anything unsaved first: a win whose move was still playing when the player paused and left.
            Action leave = () =>
            {
                results.Leave();
                services.Progress.SaveIfChanged();
                services.Scenes.Load(SceneId.LevelSelect, services);
            };
            resultPanel.Init(restart, leave, new FactCard(level.FactText, level.FactImage, level.FactImageCredit));
            playback.Init(boardView, config.Width, config.Height, results);
            pauseOverlay.Init(Pause.Resume, restart, leave);
            // Back once the level is over skips the win's wave to the result, then steps out to level select (ADR-0013),
            // after closing settings if they're open.
            pauseController.Init(Pause, () =>
            {
                if (settings.IsOpen)
                    settings.Close();
                else if (winWave.IsPlaying)
                    winWave.Skip();
                else
                    leave();
            });
            chainInput.Init(Session, boardCamera, chainView, results, Pause, services.Scenes);
        }
    }
}
