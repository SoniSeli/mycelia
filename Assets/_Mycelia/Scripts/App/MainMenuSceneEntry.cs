using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The MainMenu scene's entry point: Play opens level select (the intro first, until it is seen), and the settings
    /// button the settings modal. The main menu is the app's first screen, so back sends the app to the background once
    /// settings are closed (ADR-0013).
    /// </summary>
    public sealed class MainMenuSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField] MainMenuView view;
        [SerializeField] BackButton back;
        [SerializeField] SettingsModal settings;

        public void Construct(AppServices services, SceneArgs args)
        {
            settings.Init(choice => SettingsSwitches.Choose(services.Progress, choice, services.Haptics, services.Sounds), onClosed: null);
            // The first Play shows the intro, which then leads into level select (design §8.3 slice 8).
            view.Init(() =>
                {
                    if (services.Progress.IntroSeen)
                        services.Scenes.Load(SceneId.LevelSelect, services);
                    else
                        services.Scenes.Load(SceneId.Intro, services, new IntroSceneArgs(firstRun: true));
                },
                () => settings.Open(SettingsSwitches.ToChoice(services.Progress.Settings)));
            // Back closes settings first.
            back.Init(() =>
            {
                if (settings.IsOpen)
                    settings.Close();
                else
                    AndroidApp.MoveToBackground();
            });
            // Every button taps (ADR-0017).
            ButtonTaps.Attach(gameObject.scene, services.Sounds);
        }
    }
}
