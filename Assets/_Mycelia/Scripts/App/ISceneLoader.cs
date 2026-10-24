using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// Opens a scene in place of the current one and hands its entry the services and arguments (ADR-0010). Blocks input
    /// while the screen changes (design §8.3 slice 2).
    /// </summary>
    public interface ISceneLoader : IInputBlock
    {
        void Load(SceneId scene, AppServices services, SceneArgs args = null);
    }
}
