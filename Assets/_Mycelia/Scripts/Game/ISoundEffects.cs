namespace Mycelia.Game
{
    /// <summary>
    /// Plays sound effects (ADR-0017). The one the scenes get already follows the saved effects switch. Pausing the level
    /// stops its effects that are still sounding (ADR-0013); button taps go on, so the pause overlay's buttons are heard.
    /// Implementations never throw and allocate nothing per call after warm-up (0 B per move).
    /// </summary>
    public interface ISoundEffects : IPausable
    {
        void Play(SoundEffect effect);

        /// <summary>The chain's tick for a chain this long: the first spore plays the lowest step, each one more a step higher.</summary>
        void PlayTick(int chainLength);
    }
}
