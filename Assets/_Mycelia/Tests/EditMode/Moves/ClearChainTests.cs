using System.Collections.Generic;
using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;

namespace Mycelia.Domain.Tests.Moves
{
    public class ClearChainTests
    {
        [Test]
        public void Apply_EmptiesTheChainedCells_AndRecordsThemInChainOrder()
        {
            var board = Boards.Parse(@"
                A B
                A A");
            var chain = new[] { new GridPosition(1, 0), new GridPosition(0, 0), new GridPosition(0, 1) };
            var cleared = new List<SporeCleared>();

            ClearChain.Apply(board, chain, cleared);

            Assert.That(Boards.Print(board), Is.EqualTo(". B\n. ."));
            Assert.That(cleared, Is.EqualTo(new[]
            {
                new SporeCleared(new GridPosition(1, 0), Boards.IdOf('A')),
                new SporeCleared(new GridPosition(0, 0), Boards.IdOf('A')),
                new SporeCleared(new GridPosition(0, 1), Boards.IdOf('A')),
            }));
        }
    }
}
