namespace Mycelia.App
{
    /// <summary>
    /// Opens the intro (design §8.3 slice 8): as the first run, from the main menu's first Play, or as a replay from
    /// level select's help button.
    /// </summary>
    public sealed class IntroSceneArgs : SceneArgs
    {
        public IntroSceneArgs(bool firstRun) => FirstRun = firstRun;

        /// <summary>True when the main menu opened it; back from its first page then returns there.</summary>
        public bool FirstRun { get; }
    }
}
