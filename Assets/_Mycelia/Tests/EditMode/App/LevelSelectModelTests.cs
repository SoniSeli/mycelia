using System;
using System.Linq;
using Mycelia.Domain.Levels;
using Mycelia.Game;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class LevelSelectModelTests
    {
        static readonly string[] Order = { "level-01", "level-02", "level-03" };

        static LevelButtonState[] For(params LevelRecord[] records) =>
            LevelSelectModel.For(Order, new Progression(Order, records));

        [Test]
        public void Levels_AreNumberedFromOne_InCatalogOrder()
        {
            Assert.That(For().Select(level => level.Number), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void WithNoWins_OnlyTheFirstLevelIsOpen()
        {
            Assert.That(For().Select(level => level.Unlocked), Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void EachLevel_ShowsItsBestStars_AndWinsOpenTheNext()
        {
            LevelButtonState[] levels = For(new LevelRecord("level-01", 3), new LevelRecord("level-02", 1));

            Assert.That(levels.Select(level => level.BestStars), Is.EqualTo(new[] { 3, 1, 0 }));
            Assert.That(levels.Select(level => level.Unlocked), Is.EqualTo(new[] { true, true, true }));
        }

        [Test]
        public void RecordsForLevelsNoLongerInTheGame_AreNotShown()
        {
            Assert.That(For(new LevelRecord("level-99", 3)).Length, Is.EqualTo(3));
        }

        [Test]
        public void ACatalogWithNoLevels_ShowsNone()
        {
            Assert.That(LevelSelectModel.For(Array.Empty<string>(), new Progression(Array.Empty<string>(), Array.Empty<LevelRecord>())), Is.Empty);
        }
    }
}
