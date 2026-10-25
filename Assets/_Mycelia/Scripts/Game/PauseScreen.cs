using System;

namespace Mycelia.Game
{
    /// <summary>
    /// What covers a paused level (design §8.3 slice 5): the pause modal, or only the settings modal when the player
    /// paused to open settings. Resuming hides both, so closing settings goes straight back to play.
    /// </summary>
    public sealed class PauseScreen : IPausable
    {
        readonly IPausable pauseModal;
        readonly SettingsModal settings;
        readonly Func<SettingsChoice> current;
        bool forSettings;

        public PauseScreen(IPausable pauseModal, SettingsModal settings, Func<SettingsChoice> current)
        {
            this.pauseModal = pauseModal ?? throw new ArgumentNullException(nameof(pauseModal));
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            this.current = current ?? throw new ArgumentNullException(nameof(current));
        }

        /// <summary>The next pause shows settings instead of the pause modal.</summary>
        public void ShowSettingsOnPause() => forSettings = true;

        public void SetPaused(bool paused)
        {
            if (paused && forSettings)
            {
                settings.Open(current());
            }
            else if (paused)
            {
                pauseModal.SetPaused(true);
            }
            else
            {
                pauseModal.SetPaused(false);
                settings.Hide();
                forSettings = false;
            }
        }
    }
}
