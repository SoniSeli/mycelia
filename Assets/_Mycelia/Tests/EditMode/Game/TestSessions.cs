using System;
using System.Linq;
using Mycelia.Domain;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;

namespace Mycelia.Game.Tests
{
    /// <summary>Sessions on exact boards written as text grids, top row first; letters A–E are spore types 1–5.</summary>
    static class TestSessions
    {
        public static SporeTypeId IdOf(char letter) => new SporeTypeId((byte)(letter - 'A' + 1));

        public static Board Parse(string grid)
        {
            string[] rows = grid.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(row => row.Replace(" ", "").Trim())
                .Where(row => row.Length > 0)
                .ToArray();
            var board = new Board(rows[0].Length, rows.Length);
            for (int i = 0; i < rows.Length; i++)
            for (int column = 0; column < rows[i].Length; column++)
                board.SetOccupant(new GridPosition(column, rows.Length - 1 - i), IdOf(rows[i][column]));
            return board;
        }

        /// <summary>A session that collects 50 A spores (never won here) in <paramref name="moves"/> moves.</summary>
        public static LevelSession Start(string grid, int moves = 10)
        {
            Board board = Parse(grid);
            var config = new LevelConfig(
                "level-input", board.Width, board.Height, moves,
                "ABCDE".Select(IdOf).ToArray(),
                new[] { new ObjectiveDefinition(IdOf('A'), 50) });
            return new LevelSession(config, new Pcg32(7), board);
        }
    }
}
