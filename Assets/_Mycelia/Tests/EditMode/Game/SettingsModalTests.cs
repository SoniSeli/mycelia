using System.Collections.Generic;
using NUnit.Framework;

namespace Mycelia.Game.Tests
{
    public class SettingsModalTests
    {
        TestSettings settings;
        List<SettingsChoice> changes;
        int closes;

        [SetUp]
        public void CreateModal()
        {
            settings = new TestSettings();
            changes = new List<SettingsChoice>();
            closes = 0;
            settings.Modal.Init(changes.Add, () => closes++);
        }

        [TearDown]
        public void DestroyModal() => settings.Destroy();

        [Test]
        public void TheModal_StartsClosed()
        {
            Assert.That(settings.Modal.IsOpen, Is.False);
        }

        [Test]
        public void Opening_ShowsTheCurrentChoice_WithoutReportingAChange()
        {
            settings.Modal.Open(new SettingsChoice(effects: true, music: false, haptics: true));

            Assert.That(settings.Modal.IsOpen, Is.True);
            Assert.That(new[] { settings.Effects.isOn, settings.Music.isOn, settings.Haptics.isOn }, Is.EqualTo(new[] { true, false, true }));
            Assert.That(changes, Is.Empty);
        }

        // Design §8.3 slice 5: settings are saved on change, so every flip is reported at once.
        [Test]
        public void FlippingASwitch_ReportsTheWholeChoiceAtOnce()
        {
            settings.Modal.Open(new SettingsChoice(effects: true, music: true, haptics: true));

            settings.Music.isOn = false;

            Assert.That(changes, Is.EqualTo(new[] { new SettingsChoice(effects: true, music: false, haptics: true) }));
        }

        [TestCase("close")]
        [TestCase("backdrop")]
        public void TheCloseButton_OrATapAroundThePanel_ClosesIt(string control)
        {
            settings.Modal.Open(new SettingsChoice(true, true, true));

            (control == "close" ? settings.Close : settings.Backdrop).onClick.Invoke();

            Assert.That(settings.Modal.IsOpen, Is.False);
            Assert.That(closes, Is.EqualTo(1));
        }

        [Test]
        public void Hiding_ClosesItWithoutReportingAClose()
        {
            settings.Modal.Open(new SettingsChoice(true, true, true));

            settings.Modal.Hide();

            Assert.That(settings.Modal.IsOpen, Is.False);
            Assert.That(closes, Is.Zero);
        }
    }
}
