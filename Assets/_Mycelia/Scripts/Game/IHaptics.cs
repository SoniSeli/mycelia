namespace Mycelia.Game
{
    /// <summary>
    /// Plays haptics (ADR-0016). The one the scenes get already follows the saved haptics switch. Implementations
    /// never throw and allocate nothing per call after warm-up (0 B per move).
    /// </summary>
    public interface IHaptics
    {
        void Play(Haptic haptic);
    }
}
