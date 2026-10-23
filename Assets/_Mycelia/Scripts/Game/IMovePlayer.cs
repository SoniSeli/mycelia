using Mycelia.Domain.Moves;

namespace Mycelia.Game
{
    /// <summary>Plays back committed moves (ADR-0009). Input is locked while <see cref="IsPlaying"/> is true (ADR-0003).</summary>
    public interface IMovePlayer
    {
        bool IsPlaying { get; }

        /// <summary>Starts playing <paramref name="move"/>, which stays valid only until the next commit.</summary>
        void Play(MoveResult move);
    }
}
