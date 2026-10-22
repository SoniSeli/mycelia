namespace Mycelia.App
{
    /// <summary>
    /// A scene's single entry point (ADR-0010). The loader calls <see cref="Construct"/> once the scene has loaded,
    /// after every Awake and before any Start; the entry then hands each object in the scene what it needs.
    /// </summary>
    public interface ISceneEntry
    {
        /// <param name="args">Null when Play was pressed in this scene in the Editor: use the scene's debug args.</param>
        void Construct(AppServices services, SceneArgs args);
    }
}
