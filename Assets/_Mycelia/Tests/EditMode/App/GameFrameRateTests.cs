using System.Collections.Generic;
using Mycelia.Domain.Sessions;
using Mycelia.Game;
using NUnit.Framework;
using UnityEngine;

namespace Mycelia.App.Tests
{
    public class GameFrameRateTests
    {
        sealed class RecordingListener : IPlaybackListener
        {
            public readonly List<string> Calls = new List<string>();
            public void MovesChanged(int remaining) => Calls.Add($"moves {remaining}");
            public void ObjectiveProgressed(int index, int current, int target) => Calls.Add($"goal {index} {current}/{target}");
            public void SessionEnded(SessionState outcome, int stars) => Calls.Add($"ended {outcome} {stars}");
        }

        int before;
        RecordingListener hud;
        GameFrameRate frameRate;

        [SetUp]
        public void Create()
        {
            before = Application.targetFrameRate;
            hud = new RecordingListener();
            frameRate = new GameFrameRate(hud);
        }

        [TearDown]
        public void Restore() => Application.targetFrameRate = before;

        [Test]
        public void WhilePlaying_TheGameRunsAtTheGameplayRate()
        {
            frameRate.SetPaused(true);
            frameRate.SetPaused(false);

            Assert.That(Application.targetFrameRate, Is.EqualTo(FrameRatePolicy.Gameplay));
        }

        [Test]
        public void WhilePaused_TheGameIdlesAtTheMenuRate()
        {
            frameRate.SetPaused(true);

            Assert.That(Application.targetFrameRate, Is.EqualTo(FrameRatePolicy.Menus));
        }

        [Test]
        public void OnceTheResultShows_TheGameIdlesAtTheMenuRate_EvenAfterAResume()
        {
            frameRate.SessionEnded(SessionState.Won, 3);
            Assert.That(Application.targetFrameRate, Is.EqualTo(FrameRatePolicy.Menus));

            frameRate.SetPaused(true);
            frameRate.SetPaused(false);

            Assert.That(Application.targetFrameRate, Is.EqualTo(FrameRatePolicy.Menus));
        }

        [Test]
        public void EverythingPlaybackReports_ReachesTheHud()
        {
            frameRate.MovesChanged(7);
            frameRate.ObjectiveProgressed(1, 4, 10);
            frameRate.SessionEnded(SessionState.Lost, 0);

            Assert.That(hud.Calls, Is.EqualTo(new[] { "moves 7", "goal 1 4/10", "ended Lost 0" }));
        }
    }
}
