using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The LevelSelect scene's entry point: shows every catalog level with the player's progress, opens the Game
    /// scene on the chosen one, opens the settings modal, replays the intro from help (design §8.3 slice 8), and steps
    /// back to the main menu once settings are closed (ADR-0013).
    /// </summary>
    public sealed class LevelSelectSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField] LevelSelectView view;
        [SerializeField] BackButton back;
        [SerializeField] SettingsModal settings;

        public void Construct(AppServices services, SceneArgs args)
        {
            LevelButtonState[] levels = LevelSelectModel.For(services.Levels.Keys, services.Progress.Levels);
            settings.Init(choice => SettingsSwitches.Choose(services.Progress, choice, services.Haptics), onClosed: null);
            view.Init(levels, index => services.Scenes.Load(SceneId.Game, services, new GameSceneArgs(services.Levels.Levels[index])),
                () => settings.Open(SettingsSwitches.ToChoice(services.Progress.Settings)),
                () => services.Scenes.Load(SceneId.Intro, services, new IntroSceneArgs(firstRun: false)));
            // Back closes settings first.
            back.Init(() =>
            {
                if (settings.IsOpen)
                    settings.Close();
                else
                    services.Scenes.Load(SceneId.MainMenu, services);
            });
        }
    }
}
