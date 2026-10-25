using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The MainMenu scene's entry point: Play opens level select, and the settings button the settings modal. The main
    /// menu is the app's first screen, so back sends the app to the background once settings are closed (ADR-0013).
    /// </summary>
    public sealed class MainMenuSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField] MainMenuView view;
        [SerializeField] BackButton back;
        [SerializeField] SettingsModal settings;

        public void Construct(AppServices services, SceneArgs args)
        {
            settings.Init(choice => SettingsSwitches.Choose(services.Progress, choice), onClosed: null);
            view.Init(() => services.Scenes.Load(SceneId.LevelSelect, services),
                () => settings.Open(SettingsSwitches.ToChoice(services.Progress.Settings)));
            // Back closes settings first.
            back.Init(() =>
            {
                if (settings.IsOpen)
                    settings.Close();
                else
                    AndroidApp.MoveToBackground();
            });
        }
    }
}
