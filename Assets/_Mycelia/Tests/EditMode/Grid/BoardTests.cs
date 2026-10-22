using System;
using NUnit.Framework;
using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Tests.Grid
{
    public class BoardTests
    {
        [Test]
        public void NewBoard_HasSizeAndEmptyPlayableCells()
        {
            var board = new Board(6, 6);

            Assert.That(board.Width, Is.EqualTo(6));
            Assert.That(board.Height, Is.EqualTo(6));
            Assert.That(board.IsPlayable(new GridPosition(5, 5)), Is.True);
            Assert.That(board.GetOccupant(new GridPosition(5, 5)), Is.EqualTo(SporeTypeId.None));
        }

        [TestCase(0, 6)]
        [TestCase(6, 0)]
        [TestCase(-1, 6)]
        public void Constructor_RejectsNonPositiveSize(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(width, height));
        }

        [Test]
        public void Parse_PutsTheBottomTextRowAtRowZero()
        {
            var board = Boards.Parse(@"
                A B C
                D E F");

            Assert.That(board.GetOccupant(new GridPosition(0, 0)), Is.EqualTo(Boards.IdOf('D')));
            Assert.That(board.GetOccupant(new GridPosition(2, 1)), Is.EqualTo(Boards.IdOf('C')));
        }

        [Test]
        public void ParseThenPrint_RoundTrips()
        {
            const string text = "A . C\n# B A";

            Assert.That(Boards.Print(Boards.Parse(text)), Is.EqualTo(text));
        }

        [Test]
        public void VoidCells_AreNotPlayable()
        {
            var board = Boards.Parse(@"
                A #
                B C");

            Assert.That(board.IsPlayable(new GridPosition(1, 1)), Is.False);
            Assert.That(board.IsPlayable(new GridPosition(0, 1)), Is.True);
        }

        [Test]
        public void CellsOutsideTheBoard_AreNotPlayable()
        {
            var board = new Board(3, 3);

            Assert.That(board.IsPlayable(new GridPosition(-1, 0)), Is.False);
            Assert.That(board.IsPlayable(new GridPosition(3, 0)), Is.False);
            Assert.That(board.IsPlayable(new GridPosition(0, 3)), Is.False);
        }

        [Test]
        public void GetOccupant_RejectsCellsOutsideTheBoard()
        {
            var board = new Board(3, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetOccupant(new GridPosition(3, 0)));
        }

        [Test]
        public void SetOccupant_RejectsVoidCells()
        {
            var board = Boards.Parse("A #");

            Assert.Throws<InvalidOperationException>(() => board.SetOccupant(new GridPosition(1, 0), Boards.IdOf('B')));
        }

        [Test]
        public void SetOccupant_ThenGetOccupant_ReturnsTheSpore()
        {
            var board = new Board(2, 2);
            var cell = new GridPosition(1, 0);

            board.SetOccupant(cell, Boards.IdOf('C'));

            Assert.That(board.GetOccupant(cell), Is.EqualTo(Boards.IdOf('C')));
        }
    }
}
