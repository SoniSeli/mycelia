namespace Mycelia.App
{
    /// <summary>
    /// The launch events: <c>app_start</c> with the app version and how the save loaded, plus <c>save_recovered</c> or
    /// <c>save_reset</c> when the save had to be recovered from its backup or started over (ADR-0006).
    /// </summary>
    public static class AppStartAnalytics
    {
        public static void Report(IAnalytics analytics, string version, LoadOutcome save)
        {
            analytics.Log(new AnalyticsEvent("app_start").With("version", version).With("save", Name(save)));
            if (save == LoadOutcome.RecoveredFromBackup)
                analytics.Log(new AnalyticsEvent("save_recovered"));
            else if (save == LoadOutcome.Reset)
                analytics.Log(new AnalyticsEvent("save_reset"));
        }

        static string Name(LoadOutcome save) => save switch
        {
            LoadOutcome.NoSave => "none",
            LoadOutcome.Loaded => "loaded",
            LoadOutcome.RecoveredFromBackup => "recovered",
            LoadOutcome.Reset => "reset",
            LoadOutcome.NewerVersion => "newer_version",
            _ => save.ToString(),
        };
    }
}
