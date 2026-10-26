using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>For the Editor, other platforms and phones without a vibrator: plays nothing (ADR-0016).</summary>
    public sealed class NoHaptics : IHaptics
    {
        public void Play(Haptic haptic) { }
    }
}
