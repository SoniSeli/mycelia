using System;

namespace Mycelia.Game
{
    /// <summary>
    /// Space kept free of the board, as fractions of the safe area: a band at the top and bottom for the HUD,
    /// and a margin on each side (ADR-0011).
    /// </summary>
    public readonly struct BoardInsets
    {
        public BoardInsets(float top, float bottom, float side)
        {
            if (top < 0 || bottom < 0 || side < 0)
                throw new ArgumentException($"Insets can't be negative (top {top}, bottom {bottom}, side {side}).");
            if (top + bottom >= 1)
                throw new ArgumentException($"The top ({top}) and bottom ({bottom}) bands leave no height for the board.");
            if (side >= 0.5f)
                throw new ArgumentException($"Side margins of {side} each leave no width for the board.");

            Top = top;
            Bottom = bottom;
            Side = side;
        }

        public float Top { get; }
        public float Bottom { get; }
        public float Side { get; }
    }
}
