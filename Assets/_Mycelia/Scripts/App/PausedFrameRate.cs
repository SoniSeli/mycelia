using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>Drops a scene to the menu frame rate while it is paused, and restores its own rate on resume (ADR-0010).</summary>
    public sealed class PausedFrameRate : IPausable
    {
        readonly SceneId scene;

        public PausedFrameRate(SceneId scene) => this.scene = scene;

        public void SetPaused(bool paused) => Application.targetFrameRate = FrameRatePolicy.TargetFor(scene, paused);
    }
}
