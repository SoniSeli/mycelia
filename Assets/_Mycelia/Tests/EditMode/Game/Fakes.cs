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

    sealed class RecordingPausable : IPausable
    {
        public readonly List<bool> Calls = new List<bool>();

        public void SetPaused(bool paused) => Calls.Add(paused);
    }
}
