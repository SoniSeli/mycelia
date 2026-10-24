using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The MainMenu scene's entry point: Play opens level select. The main menu is the app's first screen, so back
    /// sends the app to the background (ADR-0013).
    /// </summary>
    public sealed class MainMenuSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField] MainMenuView view;
        [SerializeField] BackButton back;

        public void Construct(AppServices services, SceneArgs args)
        {
            view.Init(() => services.Scenes.Load(SceneId.LevelSelect, services));
            back.Init(AndroidApp.MoveToBackground);
        }
    }
}
