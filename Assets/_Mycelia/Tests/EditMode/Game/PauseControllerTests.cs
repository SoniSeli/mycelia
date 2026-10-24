using Mycelia.Domain.Grid;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class PauseControllerTests
    {
        const string Grid = @"
            A A B
            C D B";

        GameObject root;
        PauseController controller;
        int leaves;

        [SetUp]
        public void CreateController()
        {
            root = new GameObject("Pause", typeof(PauseController));
            controller = root.GetComponent<PauseController>();
            leaves = 0;
        }

        [TearDown]
        public void DestroyController() => Object.DestroyImmediate(root);

        LevelPause Start(int moves)
        {
            LevelSession session = TestSessions.Start(Grid, moves);
            var pause = new LevelPause(session, new FakePlayer());
            controller.Init(pause, () => leaves++);
            if (moves == 1)
            {
                session.Chain.Touch(new GridPosition(0, 1));
                session.Chain.Touch(new GridPosition(1, 1));
                session.Commit();
            }
            return pause;
        }

        [Test]
        public void BackDuringPlay_PausesInsteadOfLeaving()
        {
            LevelPause pause = Start(moves: 10);

            controller.Back();

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(leaves, Is.Zero);
        }

        [Test]
        public void BackOnceTheResultIsShown_LeavesTheLevel()
        {
            // ADR-0013: back steps out one layer; from the result panel that is level select.
            Start(moves: 1);

            controller.Back();

            Assert.That(leaves, Is.EqualTo(1));
        }
    }
}
