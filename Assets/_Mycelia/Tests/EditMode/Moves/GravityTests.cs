using System.Collections.Generic;
using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;

namespace Mycelia.Domain.Tests.Moves
{
    public class GravityTests
    {
        readonly List<SporeFell> falls = new List<SporeFell>();

        [SetUp]
        public void SetUp() => falls.Clear();

        static SporeFell Fell(int column, int fromRow, int toRow) =>
            new SporeFell(new GridPosition(column, fromRow), new GridPosition(column, toRow));

        [Test]
        public void Apply_LeavesNoGapsBelowSpores()
        {
            var board = Boards.Parse(@"
                A .
                . B
                B .");

            Gravity.Apply(board, falls);

            Assert.That(Boards.Print(board), Is.EqualTo(". .\nA .\nB B"));
        }

        [Test]
        public void Apply_RecordsFallsColumnByColumn_BottomToTop()
        {
            var board = Boards.Parse(@"
                A C
                B .
                . .");

            Gravity.Apply(board, falls);

            Assert.That(falls, Is.EqualTo(new[] { Fell(0, 1, 0), Fell(0, 2, 1), Fell(1, 2, 0) }));
        }

        [Test]
        public void Apply_PreservesTheOrderOfSporesWithinAColumn()
        {
            var board = Boards.Parse(@"
                A
                B
                .
                .");

            Gravity.Apply(board, falls);

            Assert.That(Boards.Print(board), Is.EqualTo(".\n.\nA\nB"));
        }

        [Test]
        public void Apply_OnASettledBoard_RecordsNothing()
        {
            var board = Boards.Parse(@"
                . B
                A B");

            Gravity.Apply(board, falls);

            Assert.That(falls, Is.Empty);
            Assert.That(Boards.Print(board), Is.EqualTo(". B\nA B"));
        }

        [Test]
        public void Apply_LetsSporesFallPastVoidCells()
        {
            var board = Boards.Parse(@"
                A
                #
                .");

            Gravity.Apply(board, falls);

            Assert.That(Boards.Print(board), Is.EqualTo(".\n#\nA"));
            Assert.That(falls, Is.EqualTo(new[] { Fell(0, 2, 0) }));
        }

        [Test]
        public void Apply_NeverMovesASporeIntoAVoidCell()
        {
            var board = Boards.Parse(@"
                A
                .
                #");

            Gravity.Apply(board, falls);

            Assert.That(Boards.Print(board), Is.EqualTo(".\nA\n#"));
        }
    }
}
