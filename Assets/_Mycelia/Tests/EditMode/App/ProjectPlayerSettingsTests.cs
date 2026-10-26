using NUnit.Framework;
using UnityEditor;

namespace Mycelia.App.Tests
{
    /// <summary>Guards the player settings that measurements and code rely on.</summary>
    public class ProjectPlayerSettingsTests
    {
        // Without it the FrameTimingManager reports nothing, and PerfProbe can't tell main-thread work from waiting.
        [Test]
        public void FrameTimingStats_AreOn_ForThePerformanceProbe()
        {
            Assert.That(PlayerSettings.enableFrameTimingStats, Is.True);
        }

        // The Unity splash added about 2 s to every cold start on the Redmi: 3.8 s to level select against a 3 s budget
        // (performance budget, Milestone 4 slice 11).
        [Test]
        public void TheUnitySplashScreen_IsOff_ForTheColdStartBudget()
        {
            Assert.That(PlayerSettings.SplashScreen.show, Is.False);
        }
    }
}
