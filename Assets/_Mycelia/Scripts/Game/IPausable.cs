namespace Mycelia.Game
{
    /// <summary>Something that stops while the level is paused and carries on when it resumes (<see cref="LevelPause"/>).</summary>
    public interface IPausable
    {
        void SetPaused(bool paused);
    }
}
