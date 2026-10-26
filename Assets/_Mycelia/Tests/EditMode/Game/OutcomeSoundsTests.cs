using System.Collections.Generic;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.Tests
{
    public class OutcomeSoundsTests
    {
        sealed class RecordingListener : IPlaybackListener
        {
            public readonly List<string> Calls = new List<string>();
            public RecordingSounds Sounds;
            public int SoundsWhenEnded = -1;

            public void MovesChanged(int remaining) => Calls.Add($"moves {remaining}");

            public void ObjectiveProgressed(int index, int current, int target) => Calls.Add($"objective {index} {current}/{target}");

            public void SessionEnded(SessionState outcome, int stars)
            {
                SoundsWhenEnded = Sounds.Played.Count;
                Calls.Add($"ended {outcome} {stars}");
            }
        }

        RecordingSounds sounds;
        RecordingListener next;
        OutcomeSounds outcome;

        [SetUp]
        public void Create()
        {
            sounds = new RecordingSounds();
            next = new RecordingListener { Sounds = sounds };
            outcome = new OutcomeSounds(sounds, next);
        }

        [Test]
        public void AWin_PlaysTheWinSound_AsTheOutcomeIsPassedOn()
        {
            outcome.SessionEnded(SessionState.Won, 2);

            Assert.That(sounds.Played, Is.EqualTo(new[] { SoundEffect.Win }));
            Assert.That(next.SoundsWhenEnded, Is.EqualTo(1));
            Assert.That(next.Calls, Is.EqualTo(new[] { "ended Won 2" }));
        }

        [Test]
        public void ALoss_PlaysTheLossSound()
        {
            outcome.SessionEnded(SessionState.Lost, 0);

            Assert.That(sounds.Played, Is.EqualTo(new[] { SoundEffect.Loss }));
            Assert.That(next.Calls, Is.EqualTo(new[] { "ended Lost 0" }));
        }

        [Test]
        public void MovesAndGoals_PassThrough_Silently()
        {
            outcome.MovesChanged(4);
            outcome.ObjectiveProgressed(1, 3, 10);

            Assert.That(sounds.Played, Is.Empty);
            Assert.That(next.Calls, Is.EqualTo(new[] { "moves 4", "objective 1 3/10" }));
        }

        [Test]
        public void PlayingTheOutcome_AllocatesNoMemory()
        {
            var quiet = new OutcomeSounds(sounds, new SilentListener());
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
