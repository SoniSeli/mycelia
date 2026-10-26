using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// The device side of sound (ADR-0017): what <see cref="SwitchedSoundEffects"/> and <see cref="Music"/> drive once
    /// the saved switches allow it. <see cref="AudioPlayer"/> plays it; tests record it.
    /// </summary>
    public interface IAudioOutput
    {
        /// <param name="chainLength">For <see cref="SoundEffect.Tick"/>, the chain's length; ignored otherwise.</param>
        void PlayEffect(SoundEffect effect, int chainLength, float volume);

        /// <summary>Stops the level's effects still sounding; taps go on.</summary>
        void StopLevelEffects();

        /// <summary>Plays the music loop at this volume, carrying on from where it paused.</summary>
        void PlayMusic(float volume);

        void PauseMusic();
    }
}
