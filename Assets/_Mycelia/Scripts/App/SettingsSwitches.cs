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

        /// <summary>
        /// The player's choice, saved at once (settings are saved on change, design §6). Switching haptics on plays one
        /// tick, so the player feels what they turned on (ADR-0016).
        /// </summary>
        public static void Choose(PlayerProgress progress, SettingsChoice choice, IHaptics haptics)
        {
            bool hapticsWereOn = progress.Settings.Haptics;
            if (progress.ChangeSettings(Apply(progress.Settings, choice)))
                progress.SaveIfChanged();
            if (choice.Haptics && !hapticsWereOn)
                haptics.Play(Haptic.Tick);
        }

        static float Volume(float current, bool on, float fallback) => !on ? 0 : current > 0 ? current : fallback;
    }
}
