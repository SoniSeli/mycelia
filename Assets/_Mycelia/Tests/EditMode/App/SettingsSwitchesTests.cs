using Mycelia.Game;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class SettingsSwitchesTests
    {
        [Test]
        public void ASoundIsOn_WhileItsVolumeIsAboveZero()
        {
            var settings = new GameSettings(sfxVolume: 0.5f, musicVolume: 0f, haptics: true);

            Assert.That(SettingsSwitches.ToChoice(settings), Is.EqualTo(new SettingsChoice(effects: true, music: false, haptics: true)));
        }

        [Test]
        public void SwitchingASoundOff_SetsItsVolumeToZero()
        {
            GameSettings result = SettingsSwitches.Apply(GameSettings.Default, new SettingsChoice(effects: false, music: true, haptics: true));

            Assert.That(result.SfxVolume, Is.Zero);
            Assert.That(result.MusicVolume, Is.EqualTo(GameSettings.Default.MusicVolume));
        }

        [Test]
        public void SwitchingASoundOn_BringsBackTheDefaultVolume_OrKeepsOneItHas()
        {
            var settings = new GameSettings(sfxVolume: 0f, musicVolume: 0.3f, haptics: false);

            GameSettings result = SettingsSwitches.Apply(settings, new SettingsChoice(effects: true, music: true, haptics: true));

            Assert.That(result.SfxVolume, Is.EqualTo(GameSettings.Default.SfxVolume));
            Assert.That(result.MusicVolume, Is.EqualTo(0.3f));
            Assert.That(result.Haptics, Is.True);
        }

        // Design §6: settings are saved when they change.
        [Test]
        public void AChoice_IsSavedAtOnce_AndTheSameChoiceWritesNothing()
        {
            var store = new FakeProgressStore();
            PlayerProgress progress = PlayerProgress.Load(store, ProjectAssets.Levels.Keys);

            SettingsSwitches.Choose(progress, new SettingsChoice(effects: true, music: false, haptics: true), new FakeHaptics());
            SettingsSwitches.Choose(progress, new SettingsChoice(effects: true, music: false, haptics: true), new FakeHaptics());

            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(store.Stored.Settings.MusicVolume, Is.Zero);
            Assert.That(progress.HasUnsavedChanges, Is.False);
        }

        // ADR-0016: the player feels what they turned on.
        [Test]
        public void SwitchingHapticsOn_PlaysOneTick_ThroughTheSwitchThatIsNowOn()
        {
            PlayerProgress progress = PlayerProgress.Load(new FakeProgressStore(), ProjectAssets.Levels.Keys);
            SettingsSwitches.Choose(progress, new SettingsChoice(effects: true, music: true, haptics: false), new FakeHaptics());
            var device = new FakeHaptics();

            SettingsSwitches.Choose(progress, new SettingsChoice(effects: true, music: true, haptics: true), new SwitchedHaptics(progress, device));

            Assert.That(device.Played, Is.EqualTo(new[] { Haptic.Tick }));
        }

        [Test]
        public void SwitchingHapticsOff_OrAnotherSwitch_PlaysNothing()
        {
            PlayerProgress progress = PlayerProgress.Load(new FakeProgressStore(), ProjectAssets.Levels.Keys);
            var haptics = new FakeHaptics();

            SettingsSwitches.Choose(progress, new SettingsChoice(effects: false, music: true, haptics: true), haptics);
            SettingsSwitches.Choose(progress, new SettingsChoice(effects: false, music: true, haptics: false), haptics);

            Assert.That(haptics.Played, Is.Empty);
        }
    }
}
