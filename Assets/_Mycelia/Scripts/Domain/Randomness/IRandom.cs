namespace Mycelia.Domain.Randomness
{
    /// <summary>
    /// Source of randomness for the domain. Injected so every board can be reproduced from its seed (ADR-0007).
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns a uniformly distributed integer in [0, <paramref name="maxExclusive"/>).</summary>
        int Next(int maxExclusive);
    }
}
