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
    /// It also logs the attempt's <c>level_end</c> once: won or lost when the deciding move has played, or, when the
    /// player leaves first (<see cref="Leave"/>), abandoned, or the real outcome if the deciding move was still playing.
    /// </summary>
    public sealed class LevelResultRecorder : IMovePlayer, IPlaybackListener
    {
        readonly LevelSession session;
        readonly PlayerProgress progress;
        readonly IMovePlayer player;
        readonly IPlaybackListener listener;
        readonly IAnalytics analytics;
        bool recorded;
        bool endLogged;

        public LevelResultRecorder(LevelSession session, PlayerProgress progress, IMovePlayer player, IPlaybackListener listener, IAnalytics analytics)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.listener = listener ?? throw new ArgumentNullException(nameof(listener));
            this.analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
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
            LogEnd();
            listener.SessionEnded(outcome, stars);
        }

        /// <summary>The player is leaving the level (Restart, Level select, or back after the result).</summary>
        public void Leave() => LogEnd();

        void LogEnd()
        {
            if (endLogged)
                return;
            endLogged = true;

            string outcome = session.State switch
            {
                SessionState.Won => "won",
                SessionState.Lost => "lost",
                _ => "abandoned",
            };
            analytics.Log(new AnalyticsEvent("level_end")
                .With("level", session.Config.Key)
                .With("outcome", outcome)
                .With("stars", session.State == SessionState.Won ? session.Stars : 0)
                .With("moves_used", session.Config.Moves - session.MovesRemaining)
                .With("moves_left", session.MovesRemaining));
        }
    }
}
