using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The LevelSelect scene's entry point: shows every catalog level with the player's progress, opens the Game
    /// scene on the chosen one, and steps back to the main menu (ADR-0013).
    /// </summary>
    public sealed class LevelSelectSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField] LevelSelectView view;
        [SerializeField] BackButton back;

        public void Construct(AppServices services, SceneArgs args)
        {
            LevelButtonState[] levels = LevelSelectModel.For(services.Levels.Keys, services.Progress.Levels);
            view.Init(levels, index => services.Scenes.Load(SceneId.Game, services, new GameSceneArgs(services.Levels.Levels[index])));
            back.Init(() => services.Scenes.Load(SceneId.MainMenu, services));
        }
    }
}
