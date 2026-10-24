using System;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;

namespace Mycelia.Balancing
{
    /// <summary>How a level went for the bot over many seeds.</summary>
    public sealed class SimulationReport
    {
        public SimulationReport(string level, int attempts, int[] starCounts, double meanMovesLeftOnWins)
        {
            Level = level;
            Attempts = attempts;
            StarCounts = starCounts;
            MeanMovesLeftOnWins = meanMovesLeftOnWins;
        }

        public string Level { get; }
        public int Attempts { get; }

        /// <summary>Index 0 counts losses; 1–3 count wins with that many stars.</summary>
        public int[] StarCounts { get; }

        public double MeanMovesLeftOnWins { get; }
        public int Wins => Attempts - StarCounts[0];
        public double WinRate => Attempts == 0 ? 0 : (double)Wins / Attempts;
    }

    /// <summary>
    /// Plays a level with <see cref="GreedyBot"/> once per seed (seeds 1..n, so a report can be reproduced exactly,
    /// ADR-0007) and counts wins, stars and moves left.
    /// </summary>
    public static class LevelSimulator
    {
        public static SimulationReport Run(LevelConfig level, int seeds, int firstSeed = 1)
        {
            if (seeds <= 0) throw new ArgumentOutOfRangeException(nameof(seeds), seeds, "Run at least one seed.");

            var stars = new int[4];
            long movesLeftOnWins = 0;
            var bot = new GreedyBot();
            for (int seed = firstSeed; seed < firstSeed + seeds; seed++)
            {
                var session = new LevelSession(level, new Pcg32((ulong)seed));
                while (session.State == SessionState.Playing)
                {
                    GridPosition[] chain = bot.ChooseChain(session);
                    if (chain.Length < 2)
                        throw new InvalidOperationException($"{level.Key}, seed {seed}: the bot found no chain, which the reshuffle guarantee rules out.");
                    foreach (GridPosition cell in chain)
                        session.Chain.Touch(cell);
                    session.Commit();
                }

                stars[session.Stars]++;
                if (session.State == SessionState.Won)
                    movesLeftOnWins += session.MovesRemaining;
            }

            int wins = seeds - stars[0];
            return new SimulationReport(level.Key, seeds, stars, wins == 0 ? 0 : (double)movesLeftOnWins / wins);
        }
    }
}
