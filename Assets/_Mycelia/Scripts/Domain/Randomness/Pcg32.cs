using System;

namespace Mycelia.Domain.Randomness
{
    /// <summary>
    /// PCG32 (XSH RR, 64-bit state) by M. E. O'Neill, pcg-random.org.
    /// Same seed and stream give the same sequence on every platform; no allocations after construction.
    /// </summary>
    public sealed class Pcg32 : IRandom
    {
        const ulong Multiplier = 6364136223846793005UL;

        ulong state;
        readonly ulong increment;

        public Pcg32(ulong seed, ulong stream = 54)
        {
            // pcg32_srandom_r: the increment must be odd; the stream selects one of 2^63 sequences.
            state = 0;
            increment = (stream << 1) | 1UL;
            NextUInt();
            state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong oldState = state;
            state = unchecked(oldState * Multiplier + increment);

            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Must be greater than zero.");

            // pcg32_boundedrand_r: reject the low values that would bias a plain modulo.
            uint bound = (uint)maxExclusive;
            uint threshold = unchecked(0u - bound) % bound;
            while (true)
            {
                uint value = NextUInt();
                if (value >= threshold)
                    return (int)(value % bound);
            }
        }
    }
}
