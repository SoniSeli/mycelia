using System.Collections.Generic;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.Tests
{
    public class ChainDragTests
    {
        // (0,0) is the bottom-left A. The top row is five A's for swipes; (1,0) and (0,1) are A's beside the diagonal.
        const string Grid = @"
            A A A A A
            A A C D B
            A A B C D";

        sealed class RecordingFeedback : IChainFeedback
        {
            public readonly List<(ChainChange Change, GridPosition Cell)> Changes = new List<(ChainChange, GridPosition)>();
            public int Ended;
            public bool Recording = true;

            public void ChainChanged(Chain chain, ChainChange change, GridPosition cell)
            {
                if (Recording) Changes.Add((change, cell));
            }

            public void ChainEnded(Chain chain) => Ended++;
        }

        sealed class FakePlayer : IMovePlayer
        {
            public int Played;
            public bool IsPlaying { get; set; }

            public void Play(MoveResult move)
            {
                Played++;
                IsPlaying = true;
            }
        }

        LevelSession session;
        RecordingFeedback feedback;
        FakePlayer player;
        ChainDrag drag;

        static Vector2 Centre(int column, int row) => new Vector2(column + 0.5f, row + 0.5f);

        static GridPosition At(int column, int row) => new GridPosition(column, row);

        [SetUp]
        public void StartDrag()
        {
            session = TestSessions.Start(Grid);
            feedback = new RecordingFeedback();
            player = new FakePlayer();
            drag = new ChainDrag(session, feedback, player, hitRadius: 0.4f, sampleStep: 0.5f);
        }

        [Test]
        public void PressingOnASpore_StartsAChain()
        {
            drag.Press(Centre(0, 0));

            Assert.That(drag.IsDragging, Is.True);
            Assert.That(session.Chain.Cells, Is.EqualTo(new[] { At(0, 0) }));
            Assert.That(feedback.Changes, Is.EqualTo(new[] { (ChainChange.Started, At(0, 0)) }));
        }

        [Test]
        public void ADragThatStartsBetweenCells_StartsAtTheFirstSporeItReaches()
        {
            drag.Press(new Vector2(1f, 1f));
            Assert.That(session.Chain.Count, Is.Zero);

            drag.Move(Centre(1, 1));

            Assert.That(session.Chain.Cells, Is.EqualTo(new[] { At(1, 1) }));
        }

        [Test]
        public void DraggingDiagonally_ChainsTheDiagonalSpore_NotTheSideNeighbours()
        {
            // A real finger wobbles: this path cuts through the corner of the side neighbour (1, 0).
            drag.Press(Centre(0, 0));
            drag.Move(new Vector2(1.1f, 0.9f));
            drag.Move(Centre(1, 1));

            Assert.That(session.Chain.Cells, Is.EqualTo(new[] { At(0, 0), At(1, 1) }));
        }

        [Test]
        public void AFastSwipeInOneFrame_ChainsEverySporeItCrosses()
        {
            drag.Press(Centre(0, 2));
            drag.Move(Centre(4, 2));

            Assert.That(session.Chain.Cells, Is.EqualTo(new[] { At(0, 2), At(1, 2), At(2, 2), At(3, 2), At(4, 2) }));
        }

        [Test]
        public void DraggingBackOntoThePreviousSpore_RemovesTheLastOne()
        {
            drag.Press(Centre(0, 0));
            drag.Move(Centre(1, 0));
            drag.Move(Centre(0, 0));

            Assert.That(session.Chain.Cells, Is.EqualTo(new[] { At(0, 0) }));
            Assert.That(feedback.Changes[feedback.Changes.Count - 1], Is.EqualTo((ChainChange.Backtracked, At(0, 0))));
        }

        [Test]
        public void ASporeOfAnotherType_IsRejected_AndReported()
        {
            drag.Press(Centre(1, 0));
            drag.Move(Centre(2, 0));

            Assert.That(session.Chain.Cells, Is.EqualTo(new[] { At(1, 0) }));
            Assert.That(feedback.Changes[feedback.Changes.Count - 1], Is.EqualTo((ChainChange.Rejected, At(2, 0))));
        }

        [Test]
        public void StayingOnTheSameSpore_ReportsNothingNew()
        {
            drag.Press(Centre(0, 0));
            drag.Move(Centre(0, 0) + new Vector2(0.1f, 0.1f));

            Assert.That(feedback.Changes.Count, Is.EqualTo(1));
        }

        [Test]
        public void MovingWithoutAPress_DoesNothing()
        {
            drag.Move(Centre(0, 0));

            Assert.That(drag.IsDragging, Is.False);
            Assert.That(session.Chain.Count, Is.Zero);
        }

        [Test]
        public void ReleasingAChainTooShortToCommit_ClearsIt_AndCostsNoMove()
        {
            drag.Press(Centre(2, 0));

            MoveResult result = drag.Release();

            Assert.That(result, Is.Null);
            Assert.That(session.Chain.Count, Is.Zero);
            Assert.That(session.MovesRemaining, Is.EqualTo(10));
            Assert.That(drag.IsDragging, Is.False);
            Assert.That(feedback.Ended, Is.EqualTo(1));
        }

        [Test]
        public void ReleasingAValidChain_CommitsTheMove()
        {
            drag.Press(Centre(0, 0));
            drag.Move(Centre(1, 0));

            MoveResult result = drag.Release();

            Assert.That(result, Is.Not.Null);
            Assert.That(session.MovesRemaining, Is.EqualTo(9));
            Assert.That(session.Chain.Count, Is.Zero);
            Assert.That(feedback.Ended, Is.EqualTo(1));
            Assert.That(player.Played, Is.EqualTo(1), "The committed move is handed over for playback.");
        }

        [Test]
        public void ACancelledChain_PlaysNothing()
        {
            drag.Press(Centre(2, 0));
            drag.Release();

            Assert.That(player.Played, Is.Zero);
        }

        [Test]
        public void WhileAMoveIsPlaying_PressesAreIgnored_UntilItEnds()
        {
            player.IsPlaying = true;
            drag.Press(Centre(0, 0));
            drag.Release();
            Assert.That(drag.IsDragging, Is.False);
            Assert.That(session.Chain.Count, Is.Zero);

            player.IsPlaying = false;
            drag.Press(Centre(0, 0));
            Assert.That(session.Chain.Count, Is.EqualTo(1));
        }

        [Test]
        public void AFingerHeldThroughPlayback_StartsNoChain_UntilItLiftsAndPressesAgain()
        {
            player.IsPlaying = true;
            drag.Press(Centre(0, 0));

            // Playback ends while the finger is still down: the controller keeps reporting a press.
            player.IsPlaying = false;
            drag.Press(Centre(0, 0));
            Assert.That(session.Chain.Count, Is.Zero, "A touch that began during playback doesn't start a chain.");

            drag.Release();
            drag.Press(Centre(0, 0));
            Assert.That(session.Chain.Count, Is.EqualTo(1));
        }

        [Test]
        public void OnceTheLevelHasEnded_InputIsIgnored()
        {
            session = TestSessions.Start(Grid, moves: 1);
            drag = new ChainDrag(session, feedback, player, hitRadius: 0.4f, sampleStep: 0.5f);
            drag.Press(Centre(0, 0));
            drag.Move(Centre(1, 0));
            drag.Release();
            Assert.That(session.State, Is.Not.EqualTo(SessionState.Playing));

            drag.Press(Centre(0, 2));

            Assert.That(drag.IsDragging, Is.False);
            Assert.That(session.Chain.Count, Is.Zero);
        }

        [Test]
        public void DraggingAcrossTheBoard_AllocatesNoMemory()
        {
            feedback.Recording = false;
            // Warm up once, so first-use costs aren't counted.
            drag.Press(Centre(0, 2));
            drag.Move(Centre(4, 2));
            drag.Move(Centre(0, 2));
            session.Chain.Clear();

            Assert.That(() =>
            {
                drag.Move(Centre(4, 2));
                drag.Move(Centre(0, 2));
                drag.Move(Centre(4, 2));
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
