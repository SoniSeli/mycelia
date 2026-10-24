namespace Mycelia.App
{
    /// <summary>The scenes the app opens. Each name is its scene file's name; Bootstrap is build index 0 (ADR-0010).</summary>
    public enum SceneId
    {
        Bootstrap,
        Game,
        MainMenu,
        LevelSelect,
    }
}
