using System;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class FrameRatePolicyTests
    {
        [Test]
        public void Gameplay_RunsAt60Fps()
        {
            Assert.That(FrameRatePolicy.TargetFor(SceneId.Game), Is.EqualTo(60));
        }

        [Test]
        public void EveryOtherScene_RunsAt30Fps()
        {
            foreach (SceneId scene in (SceneId[])Enum.GetValues(typeof(SceneId)))
            {
                if (scene == SceneId.Game) continue;
                Assert.That(FrameRatePolicy.TargetFor(scene), Is.EqualTo(30), scene.ToString());
            }
        }

        [Test]
        public void APausedGame_IdlesAt30Fps()
        {
            Assert.That(FrameRatePolicy.TargetFor(SceneId.Game, paused: true), Is.EqualTo(30));
            Assert.That(FrameRatePolicy.TargetFor(SceneId.Game, paused: false), Is.EqualTo(60));
        }
    }
}
