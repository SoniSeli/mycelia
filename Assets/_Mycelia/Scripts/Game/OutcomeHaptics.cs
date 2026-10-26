using System;
using Mycelia.Domain.Sessions;

namespace Mycelia.Game
{
    /// <summary>
    /// Sits in front of the win wave so the outcome is felt as it shows (design §8.3 slice 9): a win as the wave starts,
    /// a loss as its card appears. Moves and goals pass straight through. Allocation-free.
    /// </summary>
    public sealed class OutcomeHaptics : IPlaybackListener
    {
        readonly IHaptics haptics;
        readonly IPlaybackListener next;

        public OutcomeHaptics(IHaptics haptics, IPlaybackListener next)
        {
            this.haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void MovesChanged(int remaining) => next.MovesChanged(remaining);

        public void ObjectiveProgressed(int index, int current, int target) => next.ObjectiveProgressed(index, current, target);

        public void SessionEnded(SessionState outcome, int stars)
        {
            if (outcome == SessionState.Won)
                haptics.Play(Haptic.Win);
            else if (outcome == SessionState.Lost)
                haptics.Play(Haptic.Loss);
            next.SessionEnded(outcome, stars);
        }
    }
}
