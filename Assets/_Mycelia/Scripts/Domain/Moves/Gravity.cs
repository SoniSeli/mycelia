using System.Collections.Generic;
using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Moves
{
    /// <summary>
    /// Design §3.3 step 2: spores fall straight down to fill gaps, keeping their order.
    /// Void cells are skipped: spores fall past them but never into them.
    /// </summary>
    public static class Gravity
    {
        /// <summary>Settles every column, left to right, recording each fall from the bottom up.</summary>
        public static void Apply(Board board, List<SporeFell> falls)
        {
            for (int column = 0; column < board.Width; column++)
            {
                SettleColumn(board, column, falls);
            }
        }

        static void SettleColumn(Board board, int column, List<SporeFell> falls)
        {
            // The lowest playable row not yet filled by a settled spore.
            int target = NextPlayableRow(board, column, 0);

            for (int row = 0; row < board.Height; row++)
            {
                var cell = new GridPosition(column, row);
                if (!board.IsPlayable(cell))
                    continue;

                SporeTypeId occupant = board.GetOccupant(cell);
                if (occupant == SporeTypeId.None)
                    continue;

                if (row != target)
                {
                    var destination = new GridPosition(column, target);
                    board.SetOccupant(destination, occupant);
                    board.SetOccupant(cell, SporeTypeId.None);
                    falls.Add(new SporeFell(cell, destination));
                }

                target = NextPlayableRow(board, column, target + 1);
            }
        }

        static int NextPlayableRow(Board board, int column, int fromRow)
        {
            int row = fromRow;
            while (row < board.Height && !board.IsPlayable(new GridPosition(column, row)))
            {
                row++;
            }
            return row;
        }
    }
}
