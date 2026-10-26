using System;

namespace Mycelia.App
{
    /// <summary>
    /// The music loop (ADR-0017): one loop for every screen, owned by <see cref="AppRoot"/> so it plays on through scene
    /// fades, at the saved music volume while it is above 0 (the settings switch), paused at 0. It keeps playing while a
    /// level is paused. <see cref="Follow"/> runs every frame and only acts when the volume changed. Allocation-free.
    /// </summary>
    public sealed class Music
    {
        readonly PlayerProgress progress;
        readonly IAudioOutput output;
        float applied = -1;

        public Music(PlayerProgress progress, IAudioOutput output)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void Follow()
        {
            float volume = progress.Settings.MusicVolume;
            if (volume == applied)
                return;

            applied = volume;
            if (volume > 0)
                output.PlayMusic(volume);
            else
                output.PauseMusic();
        }
    }
}
