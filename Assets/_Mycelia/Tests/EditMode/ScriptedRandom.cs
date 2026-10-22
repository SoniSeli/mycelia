using System;
using System.Collections.Generic;
using Mycelia.Domain.Randomness;

namespace Mycelia.Domain.Tests
{
    /// <summary>
    /// Returns preset values in order, so a test can control exactly what each draw produces
    /// and check the draw order (ADR-0007). Fails loudly if a value is out of range or runs out.
    /// </summary>
    public sealed class ScriptedRandom : IRandom
    {
        readonly Queue<int> values;

        public ScriptedRandom(params int[] values) => this.values = new Queue<int>(values);

        public int Draws { get; private set; }

        public int Next(int maxExclusive)
        {
            if (values.Count == 0)
                throw new InvalidOperationException($"ScriptedRandom ran out of values after {Draws} draws.");

            int value = values.Dequeue();
            if (value < 0 || value >= maxExclusive)
                throw new InvalidOperationException($"Scripted value {value} is outside [0, {maxExclusive}).");

            Draws++;
            return value;
        }
    }
}
