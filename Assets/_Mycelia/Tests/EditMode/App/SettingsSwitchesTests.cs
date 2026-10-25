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

            SettingsSwitches.Choose(progress, new SettingsChoice(effects: true, music: false, haptics: true));
            SettingsSwitches.Choose(progress, new SettingsChoice(effects: true, music: false, haptics: true));

            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(store.Stored.Settings.MusicVolume, Is.Zero);
            Assert.That(progress.HasUnsavedChanges, Is.False);
        }
    }
}
