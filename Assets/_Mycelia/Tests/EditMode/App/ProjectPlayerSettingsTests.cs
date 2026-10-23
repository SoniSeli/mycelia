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
    }
}
