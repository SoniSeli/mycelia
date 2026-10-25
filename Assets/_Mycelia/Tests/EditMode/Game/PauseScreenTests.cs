using NUnit.Framework;

namespace Mycelia.Game.Tests
{
    public class PauseScreenTests
    {
        sealed class FakeModal : IPausable
        {
            public bool Shown;
            public void SetPaused(bool paused) => Shown = paused;
        }

        TestSettings settings;
        FakeModal pauseModal;
        PauseScreen screen;
        readonly SettingsChoice saved = new SettingsChoice(effects: false, music: true, haptics: true);

        [SetUp]
        public void CreateScreen()
        {
            settings = new TestSettings();
            settings.Modal.Init(_ => { }, () => { });
            pauseModal = new FakeModal();
            screen = new PauseScreen(pauseModal, settings.Modal, () => saved);
        }

        [TearDown]
        public void DestroyScreen() => settings.Destroy();

        [Test]
        public void APause_ShowsThePauseModal()
        {
            screen.SetPaused(true);

            Assert.That(pauseModal.Shown, Is.True);
            Assert.That(settings.Modal.IsOpen, Is.False);
        }

        // Decided 2026-10-25 (slice 5): settings opened mid-level cover the level alone.
        [Test]
        public void APauseForSettings_ShowsOnlySettings_WithTheSavedChoice()
        {
            screen.ShowSettingsOnPause();
            screen.SetPaused(true);

            Assert.That(pauseModal.Shown, Is.False);
            Assert.That(settings.Modal.IsOpen, Is.True);
            Assert.That(settings.Effects.isOn, Is.False);
        }

        [Test]
        public void Resuming_HidesBoth_AndTheNextPauseShowsThePauseModalAgain()
        {
            screen.ShowSettingsOnPause();
            screen.SetPaused(true);

            screen.SetPaused(false);
            Assert.That(settings.Modal.IsOpen, Is.False);
            Assert.That(pauseModal.Shown, Is.False);

            screen.SetPaused(true);
            Assert.That(pauseModal.Shown, Is.True);
            Assert.That(settings.Modal.IsOpen, Is.False);
        }
    }
}
