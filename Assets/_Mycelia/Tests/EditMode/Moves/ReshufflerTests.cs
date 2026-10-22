using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Randomness;

namespace Mycelia.Domain.Tests.Moves
{
    public class ReshufflerTests
    {
        // No two neighbours share a type, but shuffling these counts (A4 B4 C2 D2) can make a pair.
        const string DeadBoard = @"
            A B A B
            C D C D
            A B A B";

        static readonly SporeTypeId[] TypesABCD = "ABCD".Select(Boards.IdOf).ToArray();

        readonly List<SporeRelocated> relocated = new List<SporeRelocated>();
        readonly List<SporeCleared> cleared = new List<SporeCleared>();
        readonly List<SporeSpawned> spawned = new List<SporeSpawned>();

        [SetUp]
        public void SetUp()
        {
            relocated.Clear();
            cleared.Clear();
            spawned.Clear();
        }

        ReshuffleOutcome Apply(Board board, IRandom random, IReadOnlyList<SporeTypeId> types) =>
            new Reshuffler(board).Apply(types, random, relocated, cleared, spawned);

        [Test]
        public void Apply_WhenAValidChainExists_DoesNothing()
        {
            var board = Boards.Parse(@"
                A A
                B C");
            var random = new ScriptedRandom();

            ReshuffleOutcome outcome = Apply(board, random, TypesABCD);

            Assert.That(outcome, Is.EqualTo(ReshuffleOutcome.NotNeeded));
            Assert.That(random.Draws, Is.Zero);
            Assert.That(Boards.Print(board), Is.EqualTo("A A\nB C"));
        }

        [Test]
        public void Apply_OnADeadBoard_ShufflesUntilAValidChainExists()
        {
            var board = Boards.Parse(DeadBoard);

            ReshuffleOutcome outcome = Apply(board, new Pcg32(seed: 5), TypesABCD);

            Assert.That(outcome, Is.EqualTo(ReshuffleOutcome.Shuffled));
            Assert.That(ValidChains.Exists(board), Is.True);
        }

        [Test]
        public void Shuffling_KeepsTheTypeCounts()
        {
            var board = Boards.Parse(DeadBoard);
            string before = Sorted(Boards.Print(board));

            Apply(board, new Pcg32(seed: 5), TypesABCD);

            Assert.That(Sorted(Boards.Print(board)), Is.EqualTo(before));
        }

        [Test]
        public void Shuffling_RecordsRelocationsThatRebuildTheNewBoard()
        {
            var original = Boards.Parse(DeadBoard);
            var board = Boards.Parse(DeadBoard);

            Apply(board, new Pcg32(seed: 5), TypesABCD);

            // Replaying the relocations on the original board must give exactly the shuffled board.
            var rebuilt = Boards.Parse(DeadBoard);
            foreach (SporeRelocated move in relocated)
            {
                Assert.That(move.From, Is.Not.EqualTo(move.To));
                rebuilt.SetOccupant(move.To, original.GetOccupant(move.From));
            }
            Assert.That(Boards.Print(rebuilt), Is.EqualTo(Boards.Print(board)));
            Assert.That(cleared, Is.Empty);
            Assert.That(spawned, Is.Empty);
        }

        [Test]
        public void Shuffling_IsDeterministicForASeed()
        {
            var first = Boards.Parse(DeadBoard);
            var second = Boards.Parse(DeadBoard);

            Apply(first, new Pcg32(seed: 77), TypesABCD);
            new Reshuffler(second).Apply(TypesABCD, new Pcg32(seed: 77),
                new List<SporeRelocated>(), new List<SporeCleared>(), new List<SporeSpawned>());

            Assert.That(Boards.Print(second), Is.EqualTo(Boards.Print(first)));
        }

        [Test]
        public void Apply_WhenNoShuffleCanWork_RegeneratesTheBoard()
        {
            // Four different types: no arrangement of these spores has a pair.
            var board = Boards.Parse(@"
                A B
                C D");
            var onlyA = new[] { Boards.IdOf('A') };

            ReshuffleOutcome outcome = Apply(board, new Pcg32(seed: 3), onlyA);

            Assert.That(outcome, Is.EqualTo(ReshuffleOutcome.Regenerated));
            Assert.That(ValidChains.Exists(board), Is.True);
            Assert.That(cleared.Count, Is.EqualTo(4));
            Assert.That(spawned.Count, Is.EqualTo(4));
            Assert.That(relocated, Is.Empty);
        }

        [Test]
        public void Apply_OnABoardShapeThatCanNeverHoldAChain_Throws()
        {
            // No two playable cells are neighbours, so no refill or shuffle can ever help.
            var board = Boards.Parse("A # B");

            Assert.Throws<InvalidOperationException>(() => Apply(board, new Pcg32(seed: 1), TypesABCD));
        }

        static string Sorted(string printed) =>
            new string(printed.Where(char.IsLetter).OrderBy(c => c).ToArray());
    }
}
