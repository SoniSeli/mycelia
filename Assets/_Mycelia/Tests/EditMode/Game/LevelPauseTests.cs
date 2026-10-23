using Mycelia.Domain.Grid;
using Mycelia.Domain.Sessions;
using NUnit.Framework;

namespace Mycelia.Game.Tests
{
    public class LevelPauseTests
    {
        const string Grid = @"
            A A B
            C D B";

        LevelSession session;
        FakePlayer player;
        RecordingPausable overlay;
        RecordingPausable playback;
        LevelPause pause;

        [SetUp]
        public void StartLevel() => Start(moves: 10);

        void Start(int moves)
        {
            session = TestSessions.Start(Grid, moves);
            player = new FakePlayer();
            overlay = new RecordingPausable();
            playback = new RecordingPausable();
            pause = new LevelPause(session, player, overlay, playback);
        }

        // Spends the level's last move, so the session ends.
        void EndTheLevel()
        {
            Start(moves: 1);
            session.Chain.Touch(new GridPosition(0, 1));
            session.Chain.Touch(new GridPosition(1, 1));
            session.Commit();
            Assert.That(session.State, Is.Not.EqualTo(SessionState.Playing));
        }

        [Test]
        public void ALevelStarts_Unpaused()
        {
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(overlay.Calls, Is.Empty);
        }

        [Test]
        public void PausingALevelInPlay_PausesEverythingItWasGiven()
        {
            pause.Pause();

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(overlay.Calls, Is.EqualTo(new[] { true }));
            Assert.That(playback.Calls, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void PausingTwice_PausesEachThingOnce()
        {
            pause.Pause();
            pause.Pause();

            Assert.That(overlay.Calls, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void Resuming_ResumesEverything()
        {
            pause.Pause();

            pause.Resume();

            Assert.That(pause.IsPaused, Is.False);
            Assert.That(overlay.Calls, Is.EqualTo(new[] { true, false }));
            Assert.That(playback.Calls, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void ResumingALevelThatIsNotPaused_DoesNothing()
        {
            pause.Resume();

            Assert.That(overlay.Calls, Is.Empty);
        }

        [Test]
        public void WhileTheLastMovePlaysBack_TheLevelCanStillPause()
        {
            EndTheLevel();
            player.IsPlaying = true;

            pause.Pause();

            Assert.That(pause.IsPaused, Is.True, "The result isn't shown until playback ends.");
        }

        [Test]
        public void OnceTheResultIsShown_TheLevelCannotPause()
        {
            EndTheLevel();

            pause.Pause();

            Assert.That(pause.IsPaused, Is.False);
            Assert.That(overlay.Calls, Is.Empty);
        }

        [Test]
        public void Back_PausesALevelInPlay()
        {
            Assert.That(pause.Back(), Is.True);
            Assert.That(pause.IsPaused, Is.True);
        }

        [Test]
        public void Back_ResumesAPausedLevel()
        {
            pause.Pause();

            Assert.That(pause.Back(), Is.True);
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void Back_OnceTheResultIsShown_IsLeftToTheApp()
        {
            EndTheLevel();

            Assert.That(pause.Back(), Is.False);
            Assert.That(pause.IsPaused, Is.False);
        }
    }
}
