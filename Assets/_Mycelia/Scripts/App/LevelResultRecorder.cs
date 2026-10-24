using System;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Sessions;
using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// Sits between chain input and move playback, and between playback and the HUD, to save a win at the ADR-0006
    /// moment: the win is recorded in memory when the winning move is committed, and written when that move has
    /// finished playing, just before the result is shown. No file work happens on a move frame; going to the
    /// background during the winning move writes it too (<see cref="AppRoot"/> saves on pause).
    /// </summary>
    public sealed class LevelResultRecorder : IMovePlayer, IPlaybackListener
    {
        readonly LevelSession session;
        readonly PlayerProgress progress;
        readonly IMovePlayer player;
        readonly IPlaybackListener listener;
        bool recorded;

        public LevelResultRecorder(LevelSession session, PlayerProgress progress, IMovePlayer player, IPlaybackListener listener)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.listener = listener ?? throw new ArgumentNullException(nameof(listener));
        }

        public bool IsPlaying => player.IsPlaying;

        public void Play(MoveResult move)
        {
            if (!recorded && session.State == SessionState.Won)
            {
                recorded = true;
                progress.RecordWin(session.Config.Key, session.Stars);
            }
            player.Play(move);
        }

        public void MovesChanged(int remaining) => listener.MovesChanged(remaining);

        public void ObjectiveProgressed(int index, int current, int target) => listener.ObjectiveProgressed(index, current, target);

        public void SessionEnded(SessionState outcome, int stars)
        {
            progress.SaveIfChanged();
            listener.SessionEnded(outcome, stars);
        }
    }
}
