using System;
using System.Linq;
using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Tests.Sessions
{
    public class LevelSessionTests
    {
        static readonly SporeTypeId[] TypesABCD = "ABCD".Select(Boards.IdOf).ToArray();
        static readonly SporeTypeId A = Boards.IdOf('A');

        // (0,0) is the bottom-left D. Chaining the three A's (0,1) → (1,1) → (2,0) is a legal move.
        const string Board3x3 = @"
            B C D
            A A C
            D B A";

        static GridPosition At(int column, int row) => new GridPosition(column, row);

        static LevelConfig Config(int width, int height, int moves, int targetA) =>
            new LevelConfig("level-test", width, height, moves, TypesABCD, new[] { new ObjectiveDefinition(A, targetA) });

        static void ChainTheThreeAs(LevelSession session)
        {
            session.Chain.Touch(At(0, 1));
            session.Chain.Touch(At(1, 1));
            session.Chain.Touch(At(2, 0));
        }

        [Test]
        public void Commit_ResolvesTheMovePhaseByPhase()
        {
            // Refill draws, in ADR-0007 order (0,2), (1,2), (2,2): C, D, B.
            var session = new LevelSession(Config(3, 3, moves: 10, targetA: 5), new ScriptedRandom(2, 3, 1), Boards.Parse(Board3x3));
            ChainTheThreeAs(session);

            MoveResult result = session.Commit();

            // Settle is absent: the refilled board already has a valid chain, so nothing was reshuffled.
            Assert.That(result.Phases(), Is.EqualTo(new[] { MovePhase.Clear, MovePhase.Gravity, MovePhase.Refill, MovePhase.Evaluate }));
            Assert.That(result.Steps<SporeCleared>(MovePhase.Clear).Select(s => s.Cell), Is.EqualTo(new[] { At(0, 1), At(1, 1), At(2, 0) }));
            Assert.That(result.Steps<SporeFell>(MovePhase.Gravity), Is.EqualTo(new[]
            {
                new SporeFell(At(0, 2), At(0, 1)),
                new SporeFell(At(1, 2), At(1, 1)),
                new SporeFell(At(2, 1), At(2, 0)),
                new SporeFell(At(2, 2), At(2, 1)),
            }));
            Assert.That(result.Steps<SporeSpawned>(MovePhase.Refill).Count, Is.EqualTo(3));
            Assert.That(Boards.Print(session.Board), Is.EqualTo("C D B\nB C D\nD B C"));
        }

        [Test]
        public void Commit_SpendsAMoveAndUpdatesObjectives()
        {
            var session = new LevelSession(Config(3, 3, moves: 10, targetA: 5), new ScriptedRandom(2, 3, 1), Boards.Parse(Board3x3));
            ChainTheThreeAs(session);

            MoveResult result = session.Commit();

            Assert.That(session.MovesRemaining, Is.EqualTo(9));
            Assert.That(result.MovesRemaining, Is.EqualTo(9));
            Assert.That(session.Objectives[0].Current, Is.EqualTo(3));
            Assert.That(result.Steps<ObjectiveProgressed>(MovePhase.Evaluate), Is.EqualTo(new[] { new ObjectiveProgressed(0, 3, 5) }));
            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
            Assert.That(session.Chain.Count, Is.Zero);
        }

        [Test]
        public void CompletingTheObjectives_WinsWithStars()
        {
            var session = new LevelSession(Config(3, 3, moves: 4, targetA: 3), new ScriptedRandom(2, 3, 1), Boards.Parse(Board3x3));
            ChainTheThreeAs(session);

            MoveResult result = session.Commit();

            // 3 of 4 moves left = 75 % >= 50 %: three stars.
            Assert.That(session.State, Is.EqualTo(SessionState.Won));
            Assert.That(session.Stars, Is.EqualTo(3));
            Assert.That(result.Steps<SessionEnded>(MovePhase.Evaluate), Is.EqualTo(new[] { new SessionEnded(SessionState.Won, 3) }));
        }

        [Test]
        public void RunningOutOfMoves_LosesWithNoStars()
        {
            var session = new LevelSession(Config(3, 3, moves: 1, targetA: 5), new ScriptedRandom(2, 3, 1), Boards.Parse(Board3x3));
            ChainTheThreeAs(session);

            MoveResult result = session.Commit();

            Assert.That(session.State, Is.EqualTo(SessionState.Lost));
            Assert.That(session.Stars, Is.Zero);
            Assert.That(result.Steps<SessionEnded>(MovePhase.Evaluate), Is.EqualTo(new[] { new SessionEnded(SessionState.Lost, 0) }));
        }

        [Test]
        public void Commit_AfterTheLevelHasEnded_Throws()
        {
            var session = new LevelSession(Config(3, 3, moves: 1, targetA: 5), new ScriptedRandom(2, 3, 1), Boards.Parse(Board3x3));
            ChainTheThreeAs(session);
            session.Commit();

            Assert.Throws<InvalidOperationException>(() => session.Commit());
        }

        [Test]
        public void Commit_WithASingleSpore_ThrowsAndCostsNoMove()
        {
            var session = new LevelSession(Config(3, 3, moves: 10, targetA: 5), new ScriptedRandom(), Boards.Parse(Board3x3));
            session.Chain.Touch(At(0, 1));

            Assert.Throws<InvalidOperationException>(() => session.Commit());
            Assert.That(session.MovesRemaining, Is.EqualTo(10));
        }

        [Test]
        public void SporesClearedByRegeneratingTheBoard_DoNotCountTowardObjectives()
        {
            // Chain the top A A. The refill draws D then A, leaving four different types: a dead board
            // no shuffle can fix. Ten shuffles (3 draws each) fail, then the board is regenerated as all A.
            var draws = new[] { 3, 0 }.Concat(Enumerable.Repeat(0, 30 + 4)).ToArray();
            var session = new LevelSession(Config(2, 2, moves: 10, targetA: 5), new ScriptedRandom(draws), Boards.Parse(@"
                A A
                B C"));
            session.Chain.Touch(At(0, 1));
            session.Chain.Touch(At(1, 1));

            MoveResult result = session.Commit();

            Assert.That(result.Phases(), Is.EqualTo(new[] { MovePhase.Clear, MovePhase.Refill, MovePhase.Settle, MovePhase.Evaluate }));
            Assert.That(result.Steps<SporeCleared>(MovePhase.Settle).Count, Is.EqualTo(4));
            Assert.That(result.Steps<SporeSpawned>(MovePhase.Settle).Count, Is.EqualTo(4));
            // The regenerated board removed an A too, but only the chain's two A's count.
            Assert.That(session.Objectives[0].Current, Is.EqualTo(2));
            Assert.That(Boards.Print(session.Board), Is.EqualTo("A A\nA A"));
        }

        [Test]
        public void ANewSession_FillsTheBoardWithAValidChain()
        {
            var session = new LevelSession(Config(6, 6, moves: 20, targetA: 10), new Pcg32(seed: 2026));

            Assert.That(Boards.Print(session.Board), Does.Not.Contain(Boards.Empty.ToString()));
            Assert.DoesNotThrow(() => MoveResultExtensions.FindPair(session.Board));
            Assert.That(session.MovesRemaining, Is.EqualTo(20));
            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
        }

        [Test]
        public void TheSameSeed_GivesTheSameStartingBoard()
        {
            var first = new LevelSession(Config(6, 6, 20, 10), new Pcg32(seed: 42));
            var second = new LevelSession(Config(6, 6, 20, 10), new Pcg32(seed: 42));

            Assert.That(Boards.Print(second.Board), Is.EqualTo(Boards.Print(first.Board)));
        }

        [Test]
        public void TheMoveResult_IsReusedBetweenCommits()
        {
            var session = new LevelSession(Config(6, 6, 20, 100), new Pcg32(seed: 9));

            var (a1, b1) = MoveResultExtensions.FindPair(session.Board);
            session.Chain.Touch(a1);
            session.Chain.Touch(b1);
            MoveResult first = session.Commit();

            var (a2, b2) = MoveResultExtensions.FindPair(session.Board);
            session.Chain.Touch(a2);
            session.Chain.Touch(b2);
            MoveResult second = session.Commit();

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.MovesRemaining, Is.EqualTo(18));
            Assert.That(second.Steps<SporeCleared>(MovePhase.Clear).Select(s => s.Cell), Is.EqualTo(new[] { a2, b2 }));
        }

        [Test]
        public void AStartingBoardOfTheWrongSize_IsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                new LevelSession(Config(3, 3, 10, 5), new ScriptedRandom(), Boards.Parse("A A")));
        }
    }
}
