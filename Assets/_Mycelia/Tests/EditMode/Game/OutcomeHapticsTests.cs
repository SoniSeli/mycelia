using System.Collections.Generic;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.Tests
{
    public class OutcomeHapticsTests
    {
        sealed class RecordingListener : IPlaybackListener
        {
            public readonly List<string> Calls = new List<string>();
            public RecordingHaptics Haptics;
            public int HapticsWhenEnded = -1;

            public void MovesChanged(int remaining) => Calls.Add($"moves {remaining}");

            public void ObjectiveProgressed(int index, int current, int target) => Calls.Add($"objective {index} {current}/{target}");

            public void SessionEnded(SessionState outcome, int stars)
            {
                HapticsWhenEnded = Haptics.Played.Count;
                Calls.Add($"ended {outcome} {stars}");
            }
        }

        RecordingHaptics haptics;
        RecordingListener next;
        OutcomeHaptics outcome;

        [SetUp]
        public void Create()
        {
            haptics = new RecordingHaptics();
            next = new RecordingListener { Haptics = haptics };
            outcome = new OutcomeHaptics(haptics, next);
        }

        [Test]
        public void AWin_PlaysTheWinHaptic_AsTheOutcomeIsPassedOn()
        {
            outcome.SessionEnded(SessionState.Won, 2);

            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Win }));
            Assert.That(next.HapticsWhenEnded, Is.EqualTo(1));
            Assert.That(next.Calls, Is.EqualTo(new[] { "ended Won 2" }));
        }

        [Test]
        public void ALoss_PlaysTheLossHaptic()
        {
            outcome.SessionEnded(SessionState.Lost, 0);

            Assert.That(haptics.Played, Is.EqualTo(new[] { Haptic.Loss }));
            Assert.That(next.Calls, Is.EqualTo(new[] { "ended Lost 0" }));
        }

        [Test]
        public void MovesAndGoals_PassThrough_WithoutHaptics()
        {
            outcome.MovesChanged(7);
            outcome.ObjectiveProgressed(1, 4, 10);

            Assert.That(haptics.Played, Is.Empty);
            Assert.That(next.Calls, Is.EqualTo(new[] { "moves 7", "objective 1 4/10" }));
        }

        [Test]
        public void PlayingTheOutcome_AllocatesNoMemory()
        {
            var quiet = new OutcomeHaptics(haptics, new SilentListener());
            quiet.SessionEnded(SessionState.Won, 3);

            Assert.That(() =>
            {
                quiet.MovesChanged(3);
                quiet.SessionEnded(SessionState.Lost, 0);
            }, Is.Not.AllocatingGCMemory());
        }

        sealed class SilentListener : IPlaybackListener
        {
            public void MovesChanged(int remaining) { }
            public void ObjectiveProgressed(int index, int current, int target) { }
            public void SessionEnded(SessionState outcome, int stars) { }
        }
    }
}
