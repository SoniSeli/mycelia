using System;
using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// The sound effects every scene gets (ADR-0017): played at the saved effects volume, and only while it is above 0
    /// (the settings switch). The volume is read on every call, so a change in the settings modal applies at once.
    /// Pausing the level stops its effects. Allocation-free.
    /// </summary>
    public sealed class SwitchedSoundEffects : ISoundEffects
    {
        readonly PlayerProgress progress;
        readonly IAudioOutput output;

        public SwitchedSoundEffects(PlayerProgress progress, IAudioOutput output)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void Play(SoundEffect effect) => Play(effect, 0);

        public void PlayTick(int chainLength) => Play(SoundEffect.Tick, chainLength);

        public void SetPaused(bool paused)
        {
            // Effects are short: a paused level drops what is left of them rather than finishing them on resume.
            if (paused)
                output.StopLevelEffects();
        }

        void Play(SoundEffect effect, int chainLength)
        {
            float volume = progress.Settings.SfxVolume;
            if (volume > 0)
                output.PlayEffect(effect, chainLength, volume);
        }
    }
}
