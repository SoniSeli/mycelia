using System;
using Mycelia.Domain.Sessions;

namespace Mycelia.Game
{
    /// <summary>
    /// Sits in front of the win wave so the outcome is heard as it shows (design §8.3 slice 10): a win as the wave starts,
    /// a loss as its card appears. Moves and goals pass straight through. Allocation-free.
    /// </summary>
    public sealed class OutcomeSounds : IPlaybackListener
    {
        readonly ISoundEffects sounds;
        readonly IPlaybackListener next;

        public OutcomeSounds(ISoundEffects sounds, IPlaybackListener next)
        {
            this.sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void MovesChanged(int remaining) => next.MovesChanged(remaining);

        public void ObjectiveProgressed(int index, int current, int target) => next.ObjectiveProgressed(index, current, target);

        public void SessionEnded(SessionState outcome, int stars)
        {
            if (outcome == SessionState.Won)
                sounds.Play(SoundEffect.Win);
            else if (outcome == SessionState.Lost)
                sounds.Play(SoundEffect.Loss);
            next.SessionEnded(outcome, stars);
        }
    }
}
