using System.Collections.Generic;
using Mycelia.Domain.Moves;

namespace Mycelia.Game.Tests
{
    sealed class FakePlayer : IMovePlayer
    {
        public int Played;
        public bool IsPlaying { get; set; }

        public void Play(MoveResult move)
        {
            Played++;
            IsPlaying = true;
        }
    }

    sealed class FakeInputBlock : IInputBlock
    {
        public bool IsBlockingInput { get; set; }
    }

    // Room for every haptic a test plays, so recording allocates nothing.
    sealed class RecordingHaptics : IHaptics
    {
        public readonly List<Haptic> Played = new List<Haptic>(256);

        public void Play(Haptic haptic) => Played.Add(haptic);
    }

    // Room for every sound a test plays, so recording allocates nothing.
    sealed class RecordingSounds : ISoundEffects
    {
        public readonly List<SoundEffect> Played = new List<SoundEffect>(256);
        public readonly List<int> Ticks = new List<int>(256);
        public readonly List<bool> Paused = new List<bool>();

        public void Play(SoundEffect effect) => Played.Add(effect);

        public void PlayTick(int chainLength)
        {
            Played.Add(SoundEffect.Tick);
            Ticks.Add(chainLength);
        }

        public void SetPaused(bool paused) => Paused.Add(paused);
    }

    sealed class RecordingPausable : IPausable
    {
        public readonly List<bool> Calls = new List<bool>();

        public void SetPaused(bool paused) => Calls.Add(paused);
    }
}
