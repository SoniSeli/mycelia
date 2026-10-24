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
        [SerializeField] ResultPanel resultPanel;
        [SerializeField] PauseOverlay pauseOverlay;
        [SerializeField] PauseController pauseController;

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
            boardCamera.Init(config.Width, config.Height);
            boardView.Init(Session.Board, services.Spores);
            chainView.Init(boardView, config.Width * config.Height);
            hud.Init(config, services.Spores);
            // Play again and Restart start the same level with a fresh seed, through the loader like any scene change.
            Action restart = () => services.Scenes.Load(SceneId.Game, services, new GameSceneArgs(level));
            resultPanel.Init(restart);
            // Wins are recorded as they are committed and saved before the result shows (ADR-0006).
            var results = new LevelResultRecorder(Session, services.Progress, playback, hud);
            playback.Init(boardView, config.Width, config.Height, results);
            Pause = new LevelPause(Session, playback, playback, pauseOverlay, new PausedFrameRate(SceneId.Game));
            pauseOverlay.Init(Pause.Resume, restart);
            // Back once the result is shown steps out to level select (ADR-0013).
            pauseController.Init(Pause, () => services.Scenes.Load(SceneId.LevelSelect, services));
            chainInput.Init(Session, boardCamera, chainView, results, Pause);
        }
    }
}
