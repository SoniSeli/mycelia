using System.Linq;
using Mycelia.Balancing;
using Mycelia.Domain.Levels;
using NUnit.Framework;

namespace Mycelia.Domain.Tests.Balancing
{
    public class LevelSimulatorTests
    {
        static LevelConfig Level(int moves, int target, int types = 4) =>
            new LevelConfig("level-sim", 6, 6, moves,
                new[] { Boards.IdOf('A'), Boards.IdOf('B'), Boards.IdOf('C'), Boards.IdOf('D') }.Take(types).ToArray(),
                new[] { new ObjectiveDefinition(Boards.IdOf('A'), target) });

        [Test]
        public void AnEasyLevel_IsAlwaysWon_WithThreeStars()
        {
            SimulationReport report = LevelSimulator.Run(Level(moves: 20, target: 2), seeds: 50);

            Assert.That(report.Attempts, Is.EqualTo(50));
            Assert.That(report.Wins, Is.EqualTo(50));
            Assert.That(report.StarCounts[3], Is.EqualTo(50));
        }

        [Test]
        public void AnImpossibleLevel_IsNeverWon()
        {
            // 36 cells, 2 moves: 200 spores of one type can't be collected.
            SimulationReport report = LevelSimulator.Run(Level(moves: 2, target: 200), seeds: 20);

            Assert.That(report.Wins, Is.Zero);
            Assert.That(report.WinRate, Is.Zero);
        }

        [Test]
        public void TheSameSeeds_GiveTheSameReport()
        {
            LevelConfig level = Level(moves: 12, target: 25);

            SimulationReport first = LevelSimulator.Run(level, seeds: 40);
            SimulationReport second = LevelSimulator.Run(level, seeds: 40);

            Assert.That(second.Wins, Is.EqualTo(first.Wins));
            Assert.That(second.StarCounts, Is.EqualTo(first.StarCounts));
            Assert.That(second.MeanMovesLeftOnWins, Is.EqualTo(first.MeanMovesLeftOnWins));
        }

        [Test]
        public void StarCounts_AddUpToTheWins()
        {
            SimulationReport report = LevelSimulator.Run(Level(moves: 12, target: 25), seeds: 60);

            Assert.That(report.StarCounts[1] + report.StarCounts[2] + report.StarCounts[3], Is.EqualTo(report.Wins));
            Assert.That(report.StarCounts[0], Is.EqualTo(report.Attempts - report.Wins), "Index 0 counts the losses.");
        }

        [Test]
        public void MoreMoves_NeverMakeALevelHarder()
        {
            double tight = LevelSimulator.Run(Level(moves: 8, target: 25), seeds: 80).WinRate;
            double loose = LevelSimulator.Run(Level(moves: 14, target: 25), seeds: 80).WinRate;

            Assert.That(loose, Is.GreaterThanOrEqualTo(tight));
        }
    }
}
