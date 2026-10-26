using System;
using Mycelia.Game;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// Plays sound on the device (ADR-0017), on the <see cref="AppRoot"/> next to the one audio listener, so it outlives
    /// scene loads. Effects use <see cref="AudioSource.PlayOneShot(AudioClip, float)"/> on clips loaded up front (0 B per
    /// call on the phone after warm-up): the level's on one source, so pausing stops them together, button taps on
    /// another. The music loop streams on a third. Unity pauses all of it while the app is in the background.
    /// </summary>
    public sealed class AudioPlayer : MonoBehaviour, IAudioOutput
    {
        [SerializeField, Tooltip("The chain's ticks, lowest first: the first spore plays the first, longer chains stay on the last.")]
        AudioClip[] ticks;

        [SerializeField] AudioClip commit;
        [SerializeField] AudioClip win;
        [SerializeField] AudioClip loss;
        [SerializeField] AudioClip tap;

        [SerializeField] AudioSource levelEffects;
        [SerializeField] AudioSource taps;

        [SerializeField, Tooltip("Plays its clip on a loop.")]
        AudioSource music;

        bool musicStarted;

        /// <summary>Which tick a chain this long plays: one step per spore, held on the top step after that.</summary>
        public static int TickIndex(int chainLength, int tickCount) => Math.Max(0, Math.Min(chainLength, tickCount) - 1);

        public void PlayEffect(SoundEffect effect, int chainLength, float volume)
        {
            if (effect == SoundEffect.Tap)
                taps.PlayOneShot(tap, volume);
            else
                levelEffects.PlayOneShot(ClipFor(effect, chainLength), volume);
        }

        public void StopLevelEffects() => levelEffects.Stop();

        public void PlayMusic(float volume)
        {
            music.volume = volume;
            if (musicStarted)
            {
                music.UnPause();
                return;
            }

            musicStarted = true;
            music.Play();
        }

        public void PauseMusic() => music.Pause();

        AudioClip ClipFor(SoundEffect effect, int chainLength)
        {
            switch (effect)
            {
                case SoundEffect.Tick: return ticks[TickIndex(chainLength, ticks.Length)];
                case SoundEffect.Commit: return commit;
                case SoundEffect.Win: return win;
                case SoundEffect.Loss: return loss;
                default: throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
            }
        }
    }
}
