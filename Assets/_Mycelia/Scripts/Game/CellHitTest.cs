using Mycelia.Domain.Grid;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Maps a point on the board (in cells, see <see cref="BoardLayout"/>) to the cell it touches (ADR-0008).
    /// A cell counts only within a circle around its centre, so a finger in the corner between cells touches nothing
    /// and a diagonal drag never catches the side neighbours it passes between.
    /// </summary>
    public static class CellHitTest
    {
        public static bool TryHit(Vector2 boardPoint, int columns, int rows, float radius, out GridPosition cell)
        {
            // With a radius of at most half a cell, only the cell containing the point can be in range.
            cell = new GridPosition(Mathf.FloorToInt(boardPoint.x), Mathf.FloorToInt(boardPoint.y));
            if (cell.Column < 0 || cell.Row < 0 || cell.Column >= columns || cell.Row >= rows)
                return false;

            return (boardPoint - BoardLayout.CellCentre(cell)).sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// Points along a swipe from <paramref name="from"/> (excluded) to <paramref name="to"/> (included), at most
        /// <paramref name="step"/> apart, so a fast swipe can't jump over a spore. Allocation-free.
        /// </summary>
        public static SwipeSamples SamplePoints(Vector2 from, Vector2 to, float step) => new SwipeSamples(from, to, step);

        public readonly struct SwipeSamples
        {
            readonly Vector2 from;
            readonly Vector2 to;
            readonly int count;

            public SwipeSamples(Vector2 from, Vector2 to, float step)
            {
                this.from = from;
                this.to = to;
                count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / step));
            }

            public Enumerator GetEnumerator() => new Enumerator(from, to, count);

            public struct Enumerator
            {
                readonly Vector2 from;
                readonly Vector2 to;
                readonly int count;
                int index;

                public Enumerator(Vector2 from, Vector2 to, int count)
                {
                    this.from = from;
                    this.to = to;
                    this.count = count;
                    index = 0;
                }

                // The last sample is exactly the new point, never a rounding error short of it.
                public Vector2 Current => index == count ? to : Vector2.Lerp(from, to, (float)index / count);

                public bool MoveNext() => ++index <= count;
            }
        }
    }
}
