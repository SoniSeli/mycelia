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

        // The release keystore lives outside the repository and is set only for the length of a local release build
        // (design §8.5). Committed, it would point CI at a file it doesn't have and publish the key's path.
        [Test]
        public void TheReleaseKeystore_IsNeverSetInTheProject_SoCiSignsWithItsDebugKey()
        {
            Assert.That(PlayerSettings.Android.useCustomKeystore, Is.False);
            Assert.That(PlayerSettings.Android.keystoreName, Is.Empty);
            Assert.That(PlayerSettings.Android.keyaliasName, Is.Empty);
        }

        // Public builds carry a real version; local and CI builds share it (design §8.5).
        [Test]
        public void TheVersion_Is1_0_0_ForTheFirstPublicRelease()
        {
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("1.0.0"));
        }
    }
}
