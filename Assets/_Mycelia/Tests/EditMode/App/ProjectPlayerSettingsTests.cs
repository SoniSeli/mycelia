using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

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

        // Medium took 4.5 MiB off the APK and about 17 MB off memory against Minimal; High saved a little more, with more
        // risk of stripping code that is only reached by reflection (performance budget, Milestone 5 slice 1).
        [Test]
        public void ManagedCodeStripping_IsMedium_ForSizeAndMemory()
        {
            Assert.That(PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Android), Is.EqualTo(ManagedStrippingLevel.Medium));
        }
    }
}
