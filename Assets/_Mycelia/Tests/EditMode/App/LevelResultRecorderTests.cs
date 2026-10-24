using System.Collections.Generic;
using System.Linq;
using Mycelia.Domain;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using Mycelia.Game;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class LevelResultRecorderTests
    {
        static readonly string[] Order = { "level-01", "level-02" };
        static readonly SporeTypeId A = new SporeTypeId(1);
        static readonly SporeTypeId B = new SporeTypeId(2);

        sealed class FakePlayer : IMovePlayer
        {
            public readonly List<MoveResult> Played = new List<MoveResult>();
            public bool IsPlaying { get; set; }
            public void Play(MoveResult move) => Played.Add(move);
        }

        sealed class FakeListener : IPlaybackListener
        {
            public readonly List<string> Calls = new List<string>();
            public FakeProgressStore Store;
            public int SavesWhenShown = -1;

            public void MovesChanged(int remaining) => Calls.Add($"moves {remaining}");
            public void ObjectiveProgressed(int index, int current, int target) => Calls.Add($"objective {index} {current}/{target}");

            public void SessionEnded(SessionState outcome, int stars)
            {
                SavesWhenShown = Store.Saves;
                Calls.Add($"ended {outcome} {stars}");
            }
        }

        FakeProgressStore store;
        PlayerProgress progress;
        FakePlayer player;
        FakeListener listener;
        FakeAnalytics analytics;

        [SetUp]
        public void SetUp()
        {
            store = new FakeProgressStore();
            progress = PlayerProgress.Load(store, Order);
            player = new FakePlayer();
            listener = new FakeListener { Store = store };
            analytics = new FakeAnalytics();
        }

        // A 3 × 3 board whose top row starts with two A spores; collecting 2 A wins, collecting 50 never does.
        static LevelSession Session(string key, int target, int moves = 5)
        {
            var board = new Board(3, 3);
            for (int column = 0; column < 3; column++)
            for (int row = 0; row < 3; row++)
                board.SetOccupant(new GridPosition(column, row), B);
            board.SetOccupant(new GridPosition(0, 2), A);
            board.SetOccupant(new GridPosition(1, 2), A);

            var config = new LevelConfig(key, 3, 3, moves, new[] { A, B }, new[] { new ObjectiveDefinition(A, target) });
            return new LevelSession(config, new Pcg32(1), board);
        }

        static MoveResult CommitTheTwoAs(LevelSession session)
        {
            session.Chain.Touch(new GridPosition(0, 2));
            session.Chain.Touch(new GridPosition(1, 2));
            return session.Commit();
        }

        LevelResultRecorder Recorder(LevelSession session) => new LevelResultRecorder(session, progress, player, listener, analytics);

        [Test]
        public void TheWinningMove_IsRecordedWhenCommitted_BeforeItPlays()
        {
            LevelSession session = Session("level-01", target: 2);
            var recorder = Recorder(session);

            recorder.Play(CommitTheTwoAs(session));

            Assert.That(progress.Levels.BestStars("level-01"), Is.EqualTo(session.Stars).And.GreaterThan(0));
            Assert.That(progress.Levels.IsUnlocked("level-02"), Is.True);
            Assert.That(store.Saves, Is.Zero, "No file work on a move frame.");
            Assert.That(player.Played.Count, Is.EqualTo(1), "The move still plays.");
        }

        [Test]
        public void TheWin_IsWrittenWhenTheMoveFinishes_BeforeTheResultIsShown()
        {
            LevelSession session = Session("level-01", target: 2);
            var recorder = Recorder(session);
            recorder.Play(CommitTheTwoAs(session));
            int stars = session.Stars;

            recorder.SessionEnded(SessionState.Won, stars);

            Assert.That(listener.SavesWhenShown, Is.EqualTo(1), "Saved before the result panel is shown.");
            Assert.That(store.Stored.Levels.Single().Key, Is.EqualTo("level-01"));
            Assert.That(listener.Calls, Is.EqualTo(new[] { $"ended Won {stars}" }));
        }

        [Test]
        public void ALostLevel_RecordsAndWritesNothing()
        {
            LevelSession session = Session("level-01", target: 50, moves: 1);
            var recorder = Recorder(session);

            recorder.Play(CommitTheTwoAs(session));
            recorder.SessionEnded(SessionState.Lost, 0);

            Assert.That(session.State, Is.EqualTo(SessionState.Lost));
            Assert.That(progress.HasUnsavedChanges, Is.False);
            Assert.That(store.Saves, Is.Zero);
        }

        [Test]
        public void AMoveThatDoesNotEndTheLevel_RecordsNothing()
        {
            LevelSession session = Session("level-01", target: 50);
            var recorder = Recorder(session);

            recorder.Play(CommitTheTwoAs(session));

            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
            Assert.That(progress.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void PlaybackReports_AreForwardedToTheHud()
        {
            var recorder = Recorder(Session("level-01", target: 50));

            recorder.MovesChanged(4);
            recorder.ObjectiveProgressed(0, 2, 50);

            Assert.That(listener.Calls, Is.EqualTo(new[] { "moves 4", "objective 0 2/50" }));
        }

        [Test]
        public void IsPlaying_IsThePlayers()
        {
            var recorder = Recorder(Session("level-01", target: 50));

            player.IsPlaying = true;

            Assert.That(recorder.IsPlaying, Is.True);
        }

        [Test]
        public void AWin_LogsLevelEnd_WhenTheMoveFinishes()
        {
            LevelSession session = Session("level-01", target: 2, moves: 5);
            var recorder = Recorder(session);
            recorder.Play(CommitTheTwoAs(session));
            Assert.That(analytics.Events, Is.Empty, "Nothing is written on the move frame.");

            recorder.SessionEnded(SessionState.Won, session.Stars);

            AnalyticsEvent end = analytics.Single("level_end");
            Assert.That(end.Value("level"), Is.EqualTo("level-01"));
            Assert.That(end.Value("outcome"), Is.EqualTo("won"));
            Assert.That(end.Value("stars"), Is.EqualTo(session.Stars));
            Assert.That(end.Value("moves_used"), Is.EqualTo(1));
            Assert.That(end.Value("moves_left"), Is.EqualTo(4));
        }

        [Test]
        public void ALoss_LogsLevelEnd_Lost()
        {
            LevelSession session = Session("level-01", target: 50, moves: 1);
            var recorder = Recorder(session);
            recorder.Play(CommitTheTwoAs(session));

            recorder.SessionEnded(SessionState.Lost, 0);

            Assert.That(analytics.Single("level_end").Value("outcome"), Is.EqualTo("lost"));
            Assert.That(analytics.Single("level_end").Value("stars"), Is.EqualTo(0));
        }

        [Test]
        public void LeavingDuringPlay_LogsAbandoned()
        {
            // A level_start with no end then means the app was killed, not that the player left (ADR-0006).
            LevelSession session = Session("level-01", target: 50);
            var recorder = Recorder(session);
            recorder.Play(CommitTheTwoAs(session));

            recorder.Leave();

            Assert.That(analytics.Single("level_end").Value("outcome"), Is.EqualTo("abandoned"));
            Assert.That(analytics.Single("level_end").Value("moves_used"), Is.EqualTo(1));
        }

        [Test]
        public void LeavingAfterTheResult_LogsNoSecondEnd()
        {
            LevelSession session = Session("level-01", target: 2);
            var recorder = Recorder(session);
            recorder.Play(CommitTheTwoAs(session));
            recorder.SessionEnded(SessionState.Won, session.Stars);

            recorder.Leave();

            Assert.That(analytics.Names, Is.EqualTo(new[] { "level_end" }));
        }

        [Test]
        public void LeavingWhileTheDecidingMoveStillPlays_LogsItsRealOutcome()
        {
            LevelSession session = Session("level-01", target: 2);
            var recorder = Recorder(session);
            recorder.Play(CommitTheTwoAs(session));

            recorder.Leave();

            Assert.That(analytics.Single("level_end").Value("outcome"), Is.EqualTo("won"));
        }

        [Test]
        public void AMoveThatDoesNotEndTheLevel_LogsNothing()
        {
            LevelSession session = Session("level-01", target: 50);
            var recorder = Recorder(session);

            recorder.Play(CommitTheTwoAs(session));

            Assert.That(analytics.Events, Is.Empty);
        }
    }
}
