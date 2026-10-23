using Mycelia.Domain.Sessions;

namespace Mycelia.Game
{
    /// <summary>
    /// Told by <see cref="MovePlayback"/> what the player should see change, in step with the animation (ADR-0009):
    /// moves left as the move starts, goal progress as the cleared spores finish popping, and the outcome once the
    /// whole move has played. Implementations must not allocate (0 B per move).
    /// </summary>
    public interface IPlaybackListener
    {
        void MovesChanged(int remaining);
        void ObjectiveProgressed(int index, int current, int target);
        void SessionEnded(SessionState outcome, int stars);
    }
}
