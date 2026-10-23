using System;
using Mycelia.Domain.Sessions;

namespace Mycelia.Game
{
    /// <summary>
    /// Pauses a level and waits for the player to come back (design §6, ADR-0006): board input locks, the move being
    /// played back freezes, and the pause overlay shows. A level can pause until its result is shown, that is while
    /// it is played and while its last move plays back. It never resumes by itself.
    /// </summary>
    public sealed class LevelPause
    {
        readonly LevelSession session;
        readonly IMovePlayer player;
        readonly IPausable[] pausables;

        public LevelPause(LevelSession session, IMovePlayer player, params IPausable[] pausables)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.pausables = pausables ?? throw new ArgumentNullException(nameof(pausables));
            if (Array.IndexOf(pausables, null) >= 0)
                throw new ArgumentException("Every pausable must be set.", nameof(pausables));
        }

        public bool IsPaused { get; private set; }

        public bool CanPause => session.State == SessionState.Playing || player.IsPlaying;

        public void Pause()
        {
            if (IsPaused || !CanPause)
                return;

            IsPaused = true;
            foreach (IPausable pausable in pausables)
                pausable.SetPaused(true);
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            foreach (IPausable pausable in pausables)
                pausable.SetPaused(false);
        }

        /// <summary>The Android back button: pauses a level in play, and resumes a paused one.</summary>
        /// <returns>False once the result is shown, when there is nothing to pause: the app decides what back does.</returns>
        public bool Back()
        {
            if (IsPaused)
                Resume();
            else if (CanPause)
                Pause();
            else
                return false;
            return true;
        }
    }
}
