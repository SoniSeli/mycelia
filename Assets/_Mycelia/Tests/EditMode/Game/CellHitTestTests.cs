using System.Collections.Generic;
using Mycelia.Domain.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class CellHitTestTests
    {
        // The tuned value in ChainInputSettings, and the largest radius ChainDrag accepts.
        const float Radius = 0.45f;
        const float LargestRadius = 0.5f;

        static bool Hit(float x, float y, out GridPosition cell) => Hit(x, y, Radius, out cell);

        static bool Hit(float x, float y, float radius, out GridPosition cell) =>
            CellHitTest.TryHit(new Vector2(x, y), 6, 6, radius, out cell);

        static List<Vector2> Points(Vector2 from, Vector2 to, float step)
        {
            var points = new List<Vector2>();
            foreach (Vector2 point in CellHitTest.SamplePoints(from, to, step)) points.Add(point);
            return points;
        }

        // Every cell a swipe touches, in order, without repeats: the press point, then the samples after it.
        static List<GridPosition> CellsAlong(Vector2 from, Vector2 to, float radius = Radius)
        {
            var cells = new List<GridPosition>();
            void Touch(Vector2 point)
            {
                if (CellHitTest.TryHit(point, 6, 6, radius, out GridPosition cell) && (cells.Count == 0 || cells[cells.Count - 1] != cell))
                    cells.Add(cell);
            }

            Touch(from);
            foreach (Vector2 point in CellHitTest.SamplePoints(from, to, 0.5f)) Touch(point);
            return cells;
        }

        [Test]
        public void ATouchOnACellCentre_HitsThatCell()
        {
            Assert.That(Hit(2.5f, 3.5f, out GridPosition cell), Is.True);
            Assert.That(cell, Is.EqualTo(new GridPosition(2, 3)));
        }

        [Test]
        public void ATouchJustInsideTheRadius_Hits_AndJustOutsideMisses()
        {
            Assert.That(Hit(2.5f + 0.44f, 3.5f, out _), Is.True);
            Assert.That(Hit(2.5f + 0.46f, 3.5f, out _), Is.False);
            Assert.That(Hit(2.5f, 3.5f - 0.44f, out _), Is.True);
        }

        [Test]
        public void ATouchInTheCornerBetweenCells_HitsNothing()
        {
            Assert.That(Hit(2f, 3f, out _), Is.False);
            Assert.That(Hit(2.5f + 0.33f, 3.5f + 0.33f, out _), Is.False, "0.47 from the centre, inside the square cell");
        }

        [TestCase(Radius)]
        [TestCase(LargestRadius)]
        public void AFingerCuttingTheCornerOfASideNeighbour_HitsNothing(float radius)
        {
            // Inside cell (1, 0), near the corner it shares with (0, 0) and (1, 1): where a diagonal drag passes.
            Assert.That(Hit(1.1f, 0.9f, radius, out _), Is.False);
        }

        [Test]
        public void ATouchOffTheBoard_HitsNothing()
        {
            Assert.That(Hit(-0.4f, 0.5f, out _), Is.False);
            Assert.That(Hit(6.2f, 0.5f, out _), Is.False);
            Assert.That(Hit(0.5f, 6.1f, out _), Is.False);
        }

        [Test]
        public void SamplePoints_StepAtMostOneStepApart_AndEndOnTheNewPoint()
        {
            var from = new Vector2(0.5f, 0.5f);
            var to = new Vector2(3.0f, 0.5f);

            List<Vector2> points = Points(from, to, 0.5f);

            Assert.That(points.Count, Is.EqualTo(5));
            Assert.That(points[points.Count - 1], Is.EqualTo(to));
            Vector2 previous = from;
            foreach (Vector2 point in points)
            {
                Assert.That(Vector2.Distance(previous, point), Is.LessThanOrEqualTo(0.5f + 1e-5f));
                previous = point;
            }
        }

        [Test]
        public void AFingerThatHasNotMoved_IsSampledOnce()
        {
            var at = new Vector2(1.5f, 1.5f);

            Assert.That(Points(at, at, 0.5f), Is.EqualTo(new[] { at }));
        }

        [Test]
        public void AFastStraightSwipe_SkipsNoCell()
        {
            Assert.That(CellsAlong(new Vector2(0.5f, 2.5f), new Vector2(5.5f, 2.5f)), Is.EqualTo(new[]
            {
                new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2),
                new GridPosition(3, 2), new GridPosition(4, 2), new GridPosition(5, 2),
            }));
        }

        [TestCase(Radius)]
        [TestCase(LargestRadius)]
        public void ADiagonalSwipe_TouchesTheDiagonalCells_NeverTheSideNeighbours(float radius)
        {
            Assert.That(CellsAlong(new Vector2(0.5f, 0.5f), new Vector2(3.5f, 3.5f), radius), Is.EqualTo(new[]
            {
                new GridPosition(0, 0), new GridPosition(1, 1), new GridPosition(2, 2), new GridPosition(3, 3),
            }));
        }
    }
}
