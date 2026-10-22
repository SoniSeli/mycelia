using System;

namespace Mycelia.Domain.Grid
{
    /// <summary>A cell address on the board; (0, 0) is the bottom-left cell.</summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int Column;
        public readonly int Row;

        public GridPosition(int column, int row)
        {
            Column = column;
            Row = row;
        }

        /// <summary>
        /// True for the 8 surrounding cells: neighbours are 8-way, at Chebyshev distance 1 (design §3.2).
        /// </summary>
        public bool IsNeighbourOf(GridPosition other)
        {
            int dc = Math.Abs(Column - other.Column);
            int dr = Math.Abs(Row - other.Row);
            return Math.Max(dc, dr) == 1;
        }

        public bool Equals(GridPosition other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => (Column * 397) ^ Row;
        public override string ToString() => $"({Column}, {Row})";

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);
    }
}
