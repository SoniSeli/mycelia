using System.Linq;
using NUnit.Framework;

namespace Mycelia.Game.Tests
{
    // Design §8.3 slice 6: the main menu's mushroom breathes slowly; one breath grows it a little and back.
    public class BreathingTests
    {
        [Test]
        public void ABreath_StartsAtRestingSize_PeaksHalfway_AndEndsAtRestingSize()
        {
            Assert.That(Breathing.ScaleAt(0f, period: 3f, amount: 0.06f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Breathing.ScaleAt(1.5f, period: 3f, amount: 0.06f), Is.EqualTo(1.06f).Within(1e-5f));
            Assert.That(Breathing.ScaleAt(3f, period: 3f, amount: 0.06f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Breathing_NeverShrinksBelowRestingSize_OrGrowsPastItsPeak()
        {
            float[] scales = Enumerable.Range(0, 200).Select(i => Breathing.ScaleAt(i * 0.037f, period: 3f, amount: 0.06f)).ToArray();

            Assert.That(scales.Min(), Is.GreaterThanOrEqualTo(1f - 1e-5f));
            Assert.That(scales.Max(), Is.LessThanOrEqualTo(1.06f + 1e-5f));
        }

        [Test]
        public void Breathing_KeepsGoing_OneBreathAfterAnother()
        {
            Assert.That(Breathing.ScaleAt(31.5f, period: 3f, amount: 0.06f), Is.EqualTo(1.06f).Within(1e-4f));
        }

        [Test]
        public void Breathing_EasesInAndOut_GrowingSlowlyAtFirst()
        {
            // An eighth of a breath in: a sine ease has grown less than a seventh of the way (a steady pace would be a quarter).
            Assert.That(Breathing.ScaleAt(0.375f, period: 3f, amount: 0.06f), Is.EqualTo(1f + 0.06f * 0.14645f).Within(1e-5f));
        }
    }
}
