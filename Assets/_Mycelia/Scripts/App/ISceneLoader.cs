namespace Mycelia.App
{
    /// <summary>Opens a scene in place of the current one and hands its entry the services and arguments (ADR-0010).</summary>
    public interface ISceneLoader
    {
        void Load(SceneId scene, AppServices services, SceneArgs args = null);
    }
}
