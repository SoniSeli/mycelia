namespace Mycelia.App
{
    /// <summary>
    /// What a scene needs to know when it opens, such as the level to play. Passed through the loader, so no static
    /// field carries state between scenes (ADR-0010).
    /// </summary>
    public abstract class SceneArgs
    {
    }
}
