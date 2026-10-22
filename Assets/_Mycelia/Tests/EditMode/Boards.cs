using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Tests
{
    /// <summary>
    /// Text-grid board fixtures (ADR-0004). One character per cell, top row first, bottom row last:
    /// a letter is a spore type (A = 1, B = 2, … Z = 26), '.' is an empty cell, '#' is a void cell.
    /// Cells in a row may be separated by spaces.
    /// </summary>
    public static class Boards
    {
        public const char Empty = '.';
        public const char Void = '#';

        public static Board Parse(string text)
        {
            var rows = text
                .Split('\n')
                .Select(line => line.Replace(" ", string.Empty).Replace("\t", string.Empty).Trim())
                .Where(line => line.Length > 0)
                .ToList();

            if (rows.Count == 0)
                throw new ArgumentException("Board text has no rows.", nameof(text));

            int width = rows[0].Length;
            if (rows.Any(row => row.Length != width))
                throw new ArgumentException("All board rows must have the same width.", nameof(text));

            var board = new Board(width, rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                int row = rows.Count - 1 - i;   // text is top-down; row 0 is the bottom
                for (int column = 0; column < width; column++)
                {
                    var cell = new GridPosition(column, row);
                    char symbol = rows[i][column];
                    if (symbol == Void)
                        board.SetVoid(cell);
                    else if (symbol != Empty)
                        board.SetOccupant(cell, IdOf(symbol));
                }
            }

            return board;
        }

        public static string Print(IReadOnlyBoard board)
        {
            var lines = new List<string>(board.Height);
            for (int row = board.Height - 1; row >= 0; row--)
            {
                var line = new StringBuilder();
                for (int column = 0; column < board.Width; column++)
                {
                    if (column > 0) line.Append(' ');
                    line.Append(SymbolOf(board, new GridPosition(column, row)));
                }
                lines.Add(line.ToString());
            }
            return string.Join("\n", lines);
        }

        public static SporeTypeId IdOf(char letter)
        {
            if (letter < 'A' || letter > 'Z')
                throw new ArgumentException($"'{letter}' is not a spore letter (A–Z).", nameof(letter));
            return new SporeTypeId((byte)(letter - 'A' + 1));
        }

        static char SymbolOf(IReadOnlyBoard board, GridPosition cell)
        {
            if (!board.IsPlayable(cell)) return Void;
            SporeTypeId id = board.GetOccupant(cell);
            return id == SporeTypeId.None ? Empty : (char)('A' + id.Value - 1);
        }
    }
}
