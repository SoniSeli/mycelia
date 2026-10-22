using System;
using NUnit.Framework;
using Mycelia.Domain.Randomness;

namespace Mycelia.Domain.Tests.Randomness
{
    public class Pcg32Tests
    {
        [Test]
        public void NextUInt_MatchesPublishedReferenceSequence()
        {
            // pcg32-demo from pcg-random.org: pcg32_srandom_r(&rng, 42u, 54u)
            var rng = new Pcg32(seed: 42, stream: 54);
            uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };

            foreach (uint value in expected)
            {
                Assert.That(rng.NextUInt(), Is.EqualTo(value));
            }
        }

        [Test]
        public void SameSeedAndStream_ProduceSameSequence()
        {
            var a = new Pcg32(seed: 123456789, stream: 7);
            var b = new Pcg32(seed: 123456789, stream: 7);

            for (int i = 0; i < 100; i++)
            {
                Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()));
            }
        }

        [Test]
        public void DifferentStreams_ProduceDifferentSequences()
        {
            var a = new Pcg32(seed: 1, stream: 1);
            var b = new Pcg32(seed: 1, stream: 2);

            int equal = 0;
            for (int i = 0; i < 100; i++)
            {
                if (a.NextUInt() == b.NextUInt()) equal++;
            }

            Assert.That(equal, Is.LessThan(5));
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(7)]
        [TestCase(int.MaxValue)]
        public void Next_StaysWithinBound(int maxExclusive)
        {
            var rng = new Pcg32(seed: 2026);

            for (int i = 0; i < 10_000; i++)
            {
                int value = rng.Next(maxExclusive);
                Assert.That(value, Is.InRange(0, maxExclusive - 1));
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Next_RejectsNonPositiveBound(int maxExclusive)
        {
            var rng = new Pcg32(seed: 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(maxExclusive));
        }

        [Test]
        public void Next_IsRoughlyUniform()
        {
            const int buckets = 4;   // default number of spore types per level
            const int draws = 40_000;
            var rng = new Pcg32(seed: 99);
            var counts = new int[buckets];

            for (int i = 0; i < draws; i++)
            {
                counts[rng.Next(buckets)]++;
            }

            // Deterministic for this seed; each bucket within 3 % of the expected 10,000.
            foreach (int count in counts)
            {
                Assert.That(count, Is.InRange(9_700, 10_300));
            }
        }
    }
}
