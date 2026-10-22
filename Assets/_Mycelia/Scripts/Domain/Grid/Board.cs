using System;

namespace Mycelia.Domain.Grid
{
    /// <summary>
    /// The grid of cells. Cells are only reached through this API, so new cell layers
    /// (obstacles, hazards) can be added without changing callers (ADR-0004).
    /// </summary>
    public sealed class Board : IReadOnlyBoard
    {
        struct Cell
        {
            public bool IsVoid;
            public SporeTypeId Occupant;
        }

        readonly Cell[] cells;

        public int Width { get; }
        public int Height { get; }

        public Board(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Must be greater than zero.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Must be greater than zero.");

            Width = width;
            Height = height;
            cells = new Cell[width * height];
        }

        public bool IsInside(GridPosition cell) =>
            cell.Column >= 0 && cell.Column < Width && cell.Row >= 0 && cell.Row < Height;

        /// <summary>True if the cell is on the board and can hold a spore.</summary>
        public bool IsPlayable(GridPosition cell) => IsInside(cell) && !cells[IndexOf(cell)].IsVoid;

        /// <summary>The spore in the cell, or <see cref="SporeTypeId.None"/> if it is empty or void.</summary>
        public SporeTypeId GetOccupant(GridPosition cell) => cells[CheckedIndexOf(cell)].Occupant;

        public void SetOccupant(GridPosition cell, SporeTypeId occupant)
        {
            int index = CheckedIndexOf(cell);
            if (cells[index].IsVoid)
                throw new InvalidOperationException($"Cell {cell} is void and cannot hold a spore.");
            cells[index].Occupant = occupant;
        }

        /// <summary>Marks a cell as void: it is part of the board's shape but never holds a spore.</summary>
        public void SetVoid(GridPosition cell)
        {
            int index = CheckedIndexOf(cell);
            cells[index].IsVoid = true;
            cells[index].Occupant = SporeTypeId.None;
        }

        int CheckedIndexOf(GridPosition cell)
        {
            if (!IsInside(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), cell, $"Outside the {Width} × {Height} board.");
            return IndexOf(cell);
        }

        int IndexOf(GridPosition cell) => cell.Row * Width + cell.Column;
    }
}
