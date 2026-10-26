using System.Collections.Generic;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.Tests
{
    public class ChainHapticsTests
    {
        // (0,0) is the bottom-left A; the top row is five A's, the B at (4,1) can't join a chain of A's.
        const string Grid = @"
            A A A A A
            A C D C B
            A C B D C";

        sealed class CountingFeedback : IChainFeedback
        {
            public readonly List<ChainChange> Changes = new List<ChainChange>(64);
            public int Ended;

            public void ChainChanged(Chain chain, ChainChange change, GridPosition cell) => Changes.Add(change);

            public void ChainEnded(Chain chain) => Ended++;
        }

        LevelSession session;
        RecordingHaptics haptics;
        CountingFeedback feedback;
        FakePlayer player;
        ChainDrag drag;

        static Vector2 Centre(int column, int row) => new Vector2(column + 0.5f, row + 0.5f);

        [SetUp]
        public void StartDrag()
        {
            session = TestSessions.Start(Grid);
            haptics = new RecordingHaptics();
            feedback = new CountingFeedback();
            player = new FakePlayer();
            var chainHaptics = new ChainHaptics(haptics, feedback, player);
            drag = new ChainDrag(session, chainHaptics, chainHaptics, new LevelPause(session, player), new FakeInputBlock(),
                hitRadius: 0.4f, sampleStep: 0.25f);
        }

        [Test]
        public void EverySporeThatJoinsTheChain_Ticks()
        {
            drag.Press(Centre(0, 2));
            drag.Move(Centre(1, 2));
            drag.Move(Centre(2, 2));

            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Tick, Haptic.Tick, Haptic.Tick }));
        }

        [Test]
        public void DraggingBackOffASpore_TicksToo()
        {
            drag.Press(Centre(0, 2));
            drag.Move(Centre(1, 2));
            drag.Move(Centre(0, 2));

            Assert.That(session.Chain.Count, Is.EqualTo(1));
            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Tick, Haptic.Tick, Haptic.Tick }));
        }

        [Test]
        public void TouchingASporeThatCannotJoin_IsSilent()
        {
            drag.Press(Centre(4, 2));
            haptics.Played.Clear();

            drag.Move(Centre(4, 1));

            Assert.That(feedback.Changes[feedback.Changes.Count - 1], Is.EqualTo(ChainChange.Rejected));
            Assert.That(haptics.Played, Is.Empty);
        }

        [Test]
        public void CommittingTheChain_PlaysTheCommitHaptic_ThenHandsTheMoveOn()
        {
            drag.Press(Centre(0, 2));
            drag.Move(Centre(2, 2));
            haptics.Played.Clear();

            drag.Release();

            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Commit }));
            Assert.That(player.Played, Is.EqualTo(1));
        }

        [Test]
        public void AChainTooShortToCommit_EndsWithoutTheCommitHaptic()
        {
            drag.Press(Centre(0, 2));
            haptics.Played.Clear();

            drag.Release();

            Assert.That(haptics.Played, Is.Empty);
            Assert.That(player.Played, Is.Zero);
        }

        [Test]
        public void TheChainView_StillHearsEveryTouch_AndTheEnd()
        {
            drag.Press(Centre(3, 2));
            drag.Move(Centre(4, 2));
            drag.Move(Centre(4, 1));
            drag.Release();

            // Every sample on the rejected cell is reported.
            Assert.That(feedback.Changes.GetRange(0, 3), Is.EqualTo(new[] { ChainChange.Started, ChainChange.Extended, ChainChange.Rejected }));
            Assert.That(feedback.Changes.GetRange(2, feedback.Changes.Count - 2), Has.All.EqualTo(ChainChange.Rejected));
            Assert.That(feedback.Ended, Is.EqualTo(1));
        }

        [Test]
        public void TheMovePlayersState_IsPassedOn()
        {
            var chainHaptics = new ChainHaptics(haptics, feedback, player);

            player.IsPlaying = true;

            Assert.That(chainHaptics.IsPlaying, Is.True);
        }

        [Test]
        public void DraggingWithHaptics_AllocatesNoMemory()
        {
            // Warm up once, so first-use costs aren't counted.
            drag.Press(Centre(0, 2));
            drag.Move(Centre(4, 2));
            drag.Move(Centre(0, 2));
            session.Chain.Clear();
            haptics.Played.Clear();
            feedback.Changes.Clear();

            Assert.That(() =>
            {
                drag.Move(Centre(4, 2));
                drag.Move(Centre(0, 2));
            }, Is.Not.AllocatingGCMemory());
            Assert.That(haptics.Played, Is.Not.Empty);
        }
    }
}
