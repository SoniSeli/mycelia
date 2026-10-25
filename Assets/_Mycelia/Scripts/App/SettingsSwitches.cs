using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// Between the saved settings (volumes, ADR-0006) and the settings modal's switches (design §8.3 slice 5): a sound
    /// is on while its volume is above 0. Switching it off sets 0; switching it on keeps a volume it still has, or
    /// brings back the default one.
    /// </summary>
    public static class SettingsSwitches
    {
        public static SettingsChoice ToChoice(GameSettings settings) =>
            new SettingsChoice(settings.SfxVolume > 0, settings.MusicVolume > 0, settings.Haptics);

        public static GameSettings Apply(GameSettings settings, SettingsChoice choice) =>
            new GameSettings(
                Volume(settings.SfxVolume, choice.Effects, GameSettings.Default.SfxVolume),
                Volume(settings.MusicVolume, choice.Music, GameSettings.Default.MusicVolume),
                choice.Haptics);

        /// <summary>The player's choice, saved at once (settings are saved on change, design §6).</summary>
        public static void Choose(PlayerProgress progress, SettingsChoice choice)
        {
            if (progress.ChangeSettings(Apply(progress.Settings, choice)))
                progress.SaveIfChanged();
        }

        static float Volume(float current, bool on, float fallback) => !on ? 0 : current > 0 ? current : fallback;
    }
}
