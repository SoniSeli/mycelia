using System;
using Mycelia.Domain.Sessions;
using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The Game scene's frame rate (performance budget): the gameplay rate while the level plays, the menu rate while it
    /// is paused or once its result shows, since both screens are still (ADR-0010). Sits between the win's wave and the
    /// HUD, so the wave still plays at the gameplay rate.
    /// </summary>
    public sealed class GameFrameRate : IPausable, IPlaybackListener
    {
        readonly IPlaybackListener next;
        bool paused;
        bool ended;

        public GameFrameRate(IPlaybackListener next) => this.next = next ?? throw new ArgumentNullException(nameof(next));

        public void SetPaused(bool paused)
        {
            this.paused = paused;
            Apply();
        }

        public void MovesChanged(int remaining) => next.MovesChanged(remaining);

        public void ObjectiveProgressed(int index, int current, int target) => next.ObjectiveProgressed(index, current, target);

        public void SessionEnded(SessionState outcome, int stars)
        {
            ended = true;
            Apply();
            next.SessionEnded(outcome, stars);
        }

        void Apply() => Application.targetFrameRate = FrameRatePolicy.TargetFor(SceneId.Game, paused || ended);
    }
}
