using System;
using NUnit.Framework;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Tests.Sessions
{
    public class StarRatingTests
    {
        // Design §3.4: ★ for a win, ★★ with >= 25 % of the starting moves left, ★★★ with >= 50 %.
        [TestCase(0, 20, 1)]
        [TestCase(4, 20, 1)]
        [TestCase(5, 20, 2)]
        [TestCase(9, 20, 2)]
        [TestCase(10, 20, 3)]
        [TestCase(20, 20, 3)]
        public void For_UsesTheDefaultThresholds(int remaining, int starting, int expectedStars)
        {
            Assert.That(StarRating.For(remaining, starting, StarThresholds.Default), Is.EqualTo(expectedStars));
        }

        [TestCase(1, 1)]   // 1 of 7 = 14 %
        [TestCase(2, 2)]   // 2 of 7 = 28.6 % >= 25 %
        [TestCase(3, 2)]   // 3 of 7 = 42.9 %
        [TestCase(4, 3)]   // 4 of 7 = 57.1 % >= 50 %
        public void For_HandlesThresholdsThatAreNotWholeMoves(int remaining, int expectedStars)
        {
            Assert.That(StarRating.For(remaining, 7, StarThresholds.Default), Is.EqualTo(expectedStars));
        }

        [Test]
        public void For_UsesALevelsOwnThresholds()
        {
            var strict = new StarThresholds(twoStarPercent: 40, threeStarPercent: 75);

            Assert.That(StarRating.For(7, 20, strict), Is.EqualTo(1));
            Assert.That(StarRating.For(8, 20, strict), Is.EqualTo(2));
            Assert.That(StarRating.For(15, 20, strict), Is.EqualTo(3));
        }

        [TestCase(0, 50)]
        [TestCase(60, 50)]
        [TestCase(25, 101)]
        public void Thresholds_RejectInvalidPercentages(int two, int three)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StarThresholds(two, three));
        }

        [Test]
        public void For_RejectsMoreRemainingThanStarting()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StarRating.For(21, 20, StarThresholds.Default));
        }
    }
}
