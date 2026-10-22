using NUnit.Framework;
using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Tests.Grid
{
    public class GridPositionTests
    {
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void IsNeighbourOf_AcceptsOrthogonalNeighbours(int dc, int dr)
        {
            var centre = new GridPosition(3, 3);

            Assert.That(centre.IsNeighbourOf(new GridPosition(3 + dc, 3 + dr)), Is.True);
        }

        [TestCase(1, 1)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        [TestCase(-1, -1)]
        public void IsNeighbourOf_AcceptsDiagonalNeighbours(int dc, int dr)
        {
            // Design §3.2: neighbours are 8-way (Chebyshev distance 1).
            var centre = new GridPosition(3, 3);

            Assert.That(centre.IsNeighbourOf(new GridPosition(3 + dc, 3 + dr)), Is.True);
        }

        [Test]
        public void IsNeighbourOf_RejectsItself()
        {
            var cell = new GridPosition(2, 2);

            Assert.That(cell.IsNeighbourOf(cell), Is.False);
        }

        [TestCase(2, 0)]
        [TestCase(0, 2)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        public void IsNeighbourOf_RejectsCellsTwoOrMoreApart(int dc, int dr)
        {
            var centre = new GridPosition(3, 3);

            Assert.That(centre.IsNeighbourOf(new GridPosition(3 + dc, 3 + dr)), Is.False);
        }

        [Test]
        public void Equality_IsByValue()
        {
            Assert.That(new GridPosition(1, 2), Is.EqualTo(new GridPosition(1, 2)));
            Assert.That(new GridPosition(1, 2) == new GridPosition(1, 2), Is.True);
            Assert.That(new GridPosition(1, 2) != new GridPosition(2, 1), Is.True);
        }
    }
}
