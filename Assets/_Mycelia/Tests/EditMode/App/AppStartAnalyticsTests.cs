using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class AppStartAnalyticsTests
    {
        FakeAnalytics analytics;

        [SetUp]
        public void CreateAnalytics() => analytics = new FakeAnalytics();

        [Test]
        public void EveryLaunch_LogsAppStart_WithVersionAndHowTheSaveLoaded()
        {
            AppStartAnalytics.Report(analytics, "0.3.0", LoadOutcome.Loaded);

            Assert.That(analytics.Names, Is.EqualTo(new[] { "app_start" }));
            Assert.That(analytics.Single("app_start").Value("version"), Is.EqualTo("0.3.0"));
            Assert.That(analytics.Single("app_start").Value("save"), Is.EqualTo("loaded"));
        }

        [Test]
        public void ASaveLoadedFromTheBackup_AlsoLogsSaveRecovered()
        {
            // ADR-0006: a recovery or reset is logged, so a broken save shows up in playtest data.
            AppStartAnalytics.Report(analytics, "0.3.0", LoadOutcome.RecoveredFromBackup);

            Assert.That(analytics.Names, Is.EqualTo(new[] { "app_start", "save_recovered" }));
        }

        [Test]
        public void ASaveThatCouldNotBeRead_AlsoLogsSaveReset()
        {
            AppStartAnalytics.Report(analytics, "0.3.0", LoadOutcome.Reset);

            Assert.That(analytics.Names, Is.EqualTo(new[] { "app_start", "save_reset" }));
        }

        [TestCase(LoadOutcome.NoSave, "none")]
        [TestCase(LoadOutcome.NewerVersion, "newer_version")]
        public void OtherLoads_OnlyLogAppStart(LoadOutcome outcome, string save)
        {
            AppStartAnalytics.Report(analytics, "0.3.0", outcome);

            Assert.That(analytics.Names, Is.EqualTo(new[] { "app_start" }));
            Assert.That(analytics.Single("app_start").Value("save"), Is.EqualTo(save));
        }
    }
}
