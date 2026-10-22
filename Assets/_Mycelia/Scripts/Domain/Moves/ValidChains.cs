using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Moves
{
    /// <summary>
    /// Whether the player has any move left. With a minimum chain of 2 (design §3.2),
    /// one pair of same-type neighbours is enough.
    /// </summary>
    public static class ValidChains
    {
        // Half of the 8 directions: each neighbouring pair is checked once, from its lower or left cell.
        static readonly (int Column, int Row)[] ForwardDirections = { (1, 0), (0, 1), (1, 1), (-1, 1) };

        public static bool Exists(Board board)
        {
            for (int column = 0; column < board.Width; column++)
            {
                for (int row = 0; row < board.Height; row++)
                {
                    var cell = new GridPosition(column, row);
                    if (!board.IsPlayable(cell))
                        continue;

                    SporeTypeId type = board.GetOccupant(cell);
                    if (type == SporeTypeId.None)
                        continue;

                    foreach (var (dc, dr) in ForwardDirections)
                    {
                        var neighbour = new GridPosition(column + dc, row + dr);
                        if (board.IsPlayable(neighbour) && board.GetOccupant(neighbour) == type)
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>False if no two playable cells are neighbours, so no arrangement of spores could ever form a chain.</summary>
        public static bool ShapeAllowsAChain(Board board)
        {
            for (int column = 0; column < board.Width; column++)
            {
                for (int row = 0; row < board.Height; row++)
                {
                    var cell = new GridPosition(column, row);
                    if (!board.IsPlayable(cell))
                        continue;

                    foreach (var (dc, dr) in ForwardDirections)
                    {
                        if (board.IsPlayable(new GridPosition(column + dc, row + dr)))
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
