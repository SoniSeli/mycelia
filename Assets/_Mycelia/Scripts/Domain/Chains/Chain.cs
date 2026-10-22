using System.Collections.Generic;
using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Chains
{
    /// <summary>
    /// The chain the player is dragging (design §3.2): spores of one type, each an 8-way neighbour
    /// of the previous one, no repeats. Dragging back onto the previous spore removes the last link.
    /// Allocation-free after construction.
    /// </summary>
    public sealed class Chain
    {
        /// <summary>A shorter chain is cancelled on release and costs no move.</summary>
        public const int MinimumLength = 2;

        readonly Board board;
        readonly List<GridPosition> cells;

        public Chain(Board board)
        {
            this.board = board;
            cells = new List<GridPosition>(board.Width * board.Height);
        }

        public IReadOnlyList<GridPosition> Cells => cells;
        public int Count => cells.Count;

        /// <summary>The spore type being chained, or <see cref="SporeTypeId.None"/> when the chain is empty.</summary>
        public SporeTypeId Type { get; private set; }

        public bool IsCommittable => cells.Count >= MinimumLength;

        /// <summary>Applies the finger entering <paramref name="cell"/>.</summary>
        public ChainChange Touch(GridPosition cell)
        {
            if (cells.Count == 0)
                return TryStart(cell);

            if (cell == cells[cells.Count - 1])
                return ChainChange.Unchanged;

            if (cells.Count >= 2 && cell == cells[cells.Count - 2])
            {
                cells.RemoveAt(cells.Count - 1);
                return ChainChange.Backtracked;
            }

            if (!CanExtend(cell))
                return ChainChange.Rejected;

            cells.Add(cell);
            return ChainChange.Extended;
        }

        /// <summary>True if <paramref name="cell"/> could be added to the end of the chain. Does not change the chain.</summary>
        public bool CanExtend(GridPosition cell)
        {
            if (cells.Count == 0)
                return false;

            return board.IsPlayable(cell)
                && board.GetOccupant(cell) == Type
                && cell.IsNeighbourOf(cells[cells.Count - 1])
                && !cells.Contains(cell);
        }

        public void Clear()
        {
            cells.Clear();
            Type = SporeTypeId.None;
        }

        ChainChange TryStart(GridPosition cell)
        {
            if (!board.IsPlayable(cell))
                return ChainChange.Rejected;

            SporeTypeId occupant = board.GetOccupant(cell);
            if (occupant == SporeTypeId.None)
                return ChainChange.Rejected;

            Type = occupant;
            cells.Add(cell);
            return ChainChange.Started;
        }
    }
}
