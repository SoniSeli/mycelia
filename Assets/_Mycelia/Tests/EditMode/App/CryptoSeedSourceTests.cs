using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class CryptoSeedSourceTests
    {
        static List<ulong> Draw(int count)
        {
            var source = new CryptoSeedSource();
            var seeds = new List<ulong>(count);
            for (int i = 0; i < count; i++) seeds.Add(source.Next());
            return seeds;
        }

        [Test]
        public void EveryAttempt_GetsADifferentSeed()
        {
            Assert.That(Draw(1000), Is.Unique);
        }

        [Test]
        public void Seeds_UseAll64Bits()
        {
            // Each half is zero in all 1000 seeds with probability 2^-32000, so this never fails by chance.
            List<ulong> seeds = Draw(1000);

            Assert.That(seeds.Any(seed => seed > uint.MaxValue), "The high 32 bits are never set.");
            Assert.That(seeds.Any(seed => (seed & uint.MaxValue) != 0), "The low 32 bits are never set.");
        }
    }
}
