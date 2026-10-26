using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The Intro scene's entry point (design §8.3 slice 8): welcome, how to play and the SDGs. Next on the last page
    /// counts the intro as seen, saves (ADR-0006) and opens level select; back turns back a page, and from the first
    /// page returns where the intro came from: the main menu on the first run (the intro shows again on the next Play),
    /// level select on a replay. Opened without args (the Editor's Play), it is a replay.
    /// </summary>
    public sealed class IntroSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField] IntroView view;
        [SerializeField] BackButton back;
        [SerializeField] ChainDemo demo;

        public void Construct(AppServices services, SceneArgs args)
        {
            bool firstRun = args is IntroSceneArgs intro && intro.FirstRun;
            demo.Init(services.Spores);
            view.Init(
                onFinished: () =>
                {
                    services.Progress.MarkIntroSeen();
                    services.Progress.SaveIfChanged();
                    services.Scenes.Load(SceneId.LevelSelect, services);
                },
                onLeft: () => services.Scenes.Load(firstRun ? SceneId.MainMenu : SceneId.LevelSelect, services));
            back.Init(view.Back);
        }
    }
}
