using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Randomness;

namespace Mycelia.Domain.Tests.Moves
{
    public class RefillTests
    {
        static readonly SporeTypeId[] TypesCD = { Boards.IdOf('C'), Boards.IdOf('D') };

        readonly List<SporeSpawned> spawned = new List<SporeSpawned>();

        [SetUp]
        public void SetUp() => spawned.Clear();

        [Test]
        public void Apply_FillsEveryEmptyPlayableCell_InColumnThenBottomToTopOrder()
        {
            var board = Boards.Parse(@"
                . .
                A .
                # B");
            // ADR-0007 draw order: column 0 then 1, bottom to top: (0,2), (1,1), (1,2)
            var random = new ScriptedRandom(0, 1, 0);

            Refill.Apply(board, TypesCD, random, spawned);

            Assert.That(Boards.Print(board), Is.EqualTo("C C\nA D\n# B"));
            Assert.That(random.Draws, Is.EqualTo(3));
        }

        [Test]
        public void Apply_RecordsSpawnsWithTheirDropSlotPerColumn()
        {
            var board = Boards.Parse(@"
                . .
                A .
                # B");

            Refill.Apply(board, TypesCD, new ScriptedRandom(0, 1, 0), spawned);

            // DropSlot counts from the lowest new spore in each column, for the view's drop animation.
            Assert.That(spawned, Is.EqualTo(new[]
            {
                new SporeSpawned(new GridPosition(0, 2), Boards.IdOf('C'), dropSlot: 0),
                new SporeSpawned(new GridPosition(1, 1), Boards.IdOf('D'), dropSlot: 0),
                new SporeSpawned(new GridPosition(1, 2), Boards.IdOf('C'), dropSlot: 1),
            }));
        }

        [Test]
        public void Apply_SpawnsOnlyTheLevelsSporeTypes()
        {
            var board = new Board(6, 6);

            Refill.Apply(board, TypesCD, new Pcg32(seed: 11), spawned);

            Assert.That(spawned.Count, Is.EqualTo(36));
            Assert.That(spawned.Select(s => s.Type).Distinct(), Is.SubsetOf(TypesCD));
        }

        [Test]
        public void Apply_WithTheSameSeed_FillsTheSameBoard()
        {
            var types = new[] { Boards.IdOf('A'), Boards.IdOf('B'), Boards.IdOf('C'), Boards.IdOf('D') };
            var first = new Board(6, 6);
            var second = new Board(6, 6);

            Refill.Apply(first, types, new Pcg32(seed: 2026), spawned);
            Refill.Apply(second, types, new Pcg32(seed: 2026), new List<SporeSpawned>());

            Assert.That(Boards.Print(second), Is.EqualTo(Boards.Print(first)));
        }

        [Test]
        public void Apply_OnAFullBoard_DrawsNothing()
        {
            var board = Boards.Parse("A B");
            var random = new ScriptedRandom();

            Refill.Apply(board, TypesCD, random, spawned);

            Assert.That(spawned, Is.Empty);
            Assert.That(random.Draws, Is.Zero);
        }

        [Test]
        public void Apply_RejectsAnEmptyTypeList()
        {
            Assert.Throws<ArgumentException>(() =>
                Refill.Apply(new Board(1, 1), Array.Empty<SporeTypeId>(), new Pcg32(seed: 1), spawned));
        }
    }
}
