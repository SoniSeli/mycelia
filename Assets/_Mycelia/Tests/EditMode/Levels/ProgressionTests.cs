using System;
using System.Linq;
using NUnit.Framework;
using Mycelia.Domain.Levels;

namespace Mycelia.Domain.Tests.Levels
{
    public class ProgressionTests
    {
        static readonly string[] Order = { "level-01", "level-02", "level-03" };

        static Progression Fresh() => new Progression(Order, Array.Empty<LevelRecord>());

        [Test]
        public void TheFirstLevel_IsAlwaysUnlocked()
        {
            var progression = Fresh();

            Assert.That(progression.IsUnlocked("level-01"), Is.True);
            Assert.That(progression.IsUnlocked("level-02"), Is.False);
        }

        [Test]
        public void WinningALevel_UnlocksTheNextOne()
        {
            var progression = Fresh();

            progression.RecordWin("level-01", stars: 1);

            Assert.That(progression.IsUnlocked("level-02"), Is.True);
            Assert.That(progression.IsUnlocked("level-03"), Is.False);
        }

        [Test]
        public void Unlocks_AreDerivedFromSavedRecords()
        {
            // ADR-0006: there is no "highest unlocked" field; records alone decide.
            var progression = new Progression(Order, new[] { new LevelRecord("level-01", 2), new LevelRecord("level-02", 1) });

            Assert.That(progression.IsUnlocked("level-03"), Is.True);
        }

        [Test]
        public void BestStars_OnlyGoUp()
        {
            var progression = Fresh();

            Assert.That(progression.RecordWin("level-01", stars: 2), Is.True);
            Assert.That(progression.RecordWin("level-01", stars: 1), Is.False);
            Assert.That(progression.BestStars("level-01"), Is.EqualTo(2));

            Assert.That(progression.RecordWin("level-01", stars: 3), Is.True);
            Assert.That(progression.BestStars("level-01"), Is.EqualTo(3));
        }

        [Test]
        public void ALevelNeverWon_HasZeroStars()
        {
            Assert.That(Fresh().BestStars("level-02"), Is.Zero);
        }

        [Test]
        public void RecordsForUnknownLevels_AreKeptButIgnored()
        {
            // A level removed in an update: keep its record so a later update could restore it.
            var progression = new Progression(Order, new[] { new LevelRecord("level-99", 3) });

            Assert.That(progression.IsUnlocked("level-02"), Is.False);
            Assert.That(progression.Records.Select(r => r.Key), Does.Contain("level-99"));
        }

        [Test]
        public void Records_IncludeNewWins()
        {
            var progression = Fresh();

            progression.RecordWin("level-01", stars: 3);

            Assert.That(progression.Records, Has.Exactly(1).Matches<LevelRecord>(r => r.Key == "level-01" && r.BestStars == 3));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void RecordWin_RejectsAStarCountOutsideOneToThree(int stars)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fresh().RecordWin("level-01", stars));
        }

        [Test]
        public void RecordWin_RejectsAnUnknownLevel()
        {
            Assert.Throws<ArgumentException>(() => Fresh().RecordWin("level-42", stars: 1));
        }

        [Test]
        public void RecordWin_RejectsALockedLevel()
        {
            Assert.Throws<InvalidOperationException>(() => Fresh().RecordWin("level-03", stars: 1));
        }
    }
}
