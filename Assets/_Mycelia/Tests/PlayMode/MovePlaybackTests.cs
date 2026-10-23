using System.Collections;
using Mycelia.Content;
using Mycelia.Domain;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using PrimeTween;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.PlayTests
{
    public class MovePlaybackTests
    {
        GameObject root;
        GameObject sporeTemplate;
        GameObject cellTemplate;
        PlaybackSettings settings;
        BoardView boardView;
        MovePlayback playback;
        SporeCatalog catalog;
        LevelSession session;

        static GridPosition At(int column, int row) => new GridPosition(column, row);

        [SetUp]
        public void CreateViews()
        {
            catalog = PlaySessions.ProjectSpores;
            root = new GameObject("Board");
            sporeTemplate = new GameObject("Spore Template", typeof(SpriteRenderer));
            var sporeView = sporeTemplate.AddComponent<SporeView>();
            SetReference(sporeView, "spriteRenderer", sporeTemplate.GetComponent<SpriteRenderer>());
            sporeTemplate.SetActive(false);
            cellTemplate = new GameObject("Cell Template", typeof(SpriteRenderer));
            cellTemplate.SetActive(false);

            boardView = root.AddComponent<BoardView>();
            SetReference(boardView, "sporePrefab", sporeView);
            SetReference(boardView, "cellPrefab", cellTemplate.GetComponent<SpriteRenderer>());
            settings = ScriptableObject.CreateInstance<PlaybackSettings>();
            playback = root.AddComponent<MovePlayback>();
            SetReference(playback, "settings", settings);
        }

        static void SetReference(Object target, string property, Object value)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        [TearDown]
        public void DestroyViews()
        {
            if (root != null) Object.DestroyImmediate(root);
            Object.DestroyImmediate(sporeTemplate);
            Object.DestroyImmediate(cellTemplate);
            Object.DestroyImmediate(settings);
        }

        void Start(string grid, IRandom random)
        {
            session = PlaySessions.Start(grid, random);
            boardView.Init(session.Board, catalog);
            playback.Init(boardView, session.Board.Width, session.Board.Height);
        }

        MoveResult Commit(params GridPosition[] chain)
        {
            foreach (GridPosition cell in chain) session.Chain.Touch(cell);
            return session.Commit();
        }

        void AssertViewsMatchTheBoard()
        {
            IReadOnlyBoard board = session.Board;
            int occupied = 0;
            for (int column = 0; column < board.Width; column++)
            for (int row = 0; row < board.Height; row++)
            {
                GridPosition cell = At(column, row);
                SporeView spore = boardView.SporeAt(cell);
                SporeTypeId occupant = board.GetOccupant(cell);
                if (occupant == SporeTypeId.None)
                {
                    Assert.That(spore, Is.Null, cell.ToString());
                    continue;
                }

                occupied++;
                SporeType type = catalog.TypeOf(occupant);
                Assert.That(spore, Is.Not.Null, cell.ToString());
                Assert.That(spore.gameObject.activeSelf, Is.True, $"{cell} active");
                Assert.That(spore.Renderer.sprite, Is.SameAs(type.Sprite), $"{cell} sprite");
                Assert.That(spore.Renderer.color, Is.EqualTo(type.Color), $"{cell} colour, including alpha");
                Assert.That(Vector2.Distance(spore.transform.localPosition, BoardLayout.CellCentre(cell)), Is.LessThan(1e-4f), $"{cell} position");
                Assert.That(spore.transform.localScale, Is.EqualTo(Vector3.one), $"{cell} scale");
            }

            Assert.That(boardView.Pool.Available, Is.EqualTo(boardView.Pool.Capacity - occupied), "Every cleared view is back in the pool.");
        }

        // Column 0 loses its two A's; B falls two cells; the refill (B, then C) leaves B B C, so no reshuffle.
        const string FallGrid = @"
            B C
            A D
            A A";

        [UnityTest]
        public IEnumerator AMove_PopsThenFallsThenRefills_AndEndsMatchingTheBoard()
        {
            Start(FallGrid, new ScriptedRandom(1, 2));
            SporeView falling = boardView.SporeAt(At(0, 2));

            playback.Play(Commit(At(0, 0), At(0, 1)));
            Assert.That(playback.IsPlaying, Is.True);
            SporeView spawned = boardView.SporeAt(At(0, 2));
            Assert.That(spawned.Renderer.color.a, Is.Zero, "New spores are invisible until the refill starts.");

            yield return new WaitForSeconds(settings.ClearDuration * 0.5f);
            Assert.That(falling.transform.localPosition.y, Is.EqualTo(2.5f).Within(1e-4f), "Nothing falls while spores pop.");

            yield return new WaitForSeconds(settings.ClearDuration * 0.5f + settings.FallDuration(2) * 0.5f);
            Assert.That(falling.transform.localPosition.y, Is.LessThan(2.4f), "The spore is falling after the pop.");
            Assert.That(playback.IsPlaying, Is.True, "...and has not landed yet.");

            float timeout = Time.time + 5f;
            while (playback.IsPlaying && Time.time < timeout) yield return null;
            Assert.That(playback.IsPlaying, Is.False, "Playback ends.");
            Assert.That(boardView.SporeAt(At(0, 0)), Is.SameAs(falling));
            AssertViewsMatchTheBoard();
        }

        // Refill leaves B A B, a dead board; the scripted shuffle rotates it to B B A, moving cell 2's spore to cell 0.
        [Test]
        public void AReshuffle_MovesEverySporeToItsNewCell()
        {
            Start("A A B", new ScriptedRandom(1, 0, 1, 0));
            SporeView lastB = boardView.SporeAt(At(2, 0));
            MoveResult move = Commit(At(0, 0), At(1, 0));

            playback.Play(move);
            playback.CompleteNow();

            Assert.That(boardView.SporeAt(At(0, 0)), Is.SameAs(lastB), "The B from cell 2 was moved to cell 0.");
            AssertViewsMatchTheBoard();
        }

        // Refill leaves A B; every shuffle of two different spores fails, so the board is regenerated as C C.
        [Test]
        public void ARegeneratedBoard_AfterAChain_StillHasViewsToShow()
        {
            Start("A A", new ScriptedRandom(0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2));

            playback.Play(Commit(At(0, 0), At(1, 0)));
            playback.CompleteNow();

            AssertViewsMatchTheBoard();
        }

        [Test]
        public void AFinishedMove_UnlocksInput()
        {
            Start(FallGrid, new ScriptedRandom(1, 2));

            playback.Play(Commit(At(0, 0), At(0, 1)));
            playback.CompleteNow();

            Assert.That(playback.IsPlaying, Is.False);
        }

        [Test]
        public void DestroyingThePlaybackMidMove_StopsItsTweens()
        {
            Start(FallGrid, new ScriptedRandom(1, 2));
            Transform falling = boardView.SporeAt(At(0, 2)).transform;
            playback.Play(Commit(At(0, 0), At(0, 1)));
            Assert.That(Tween.GetTweensCount(falling), Is.GreaterThan(0));

            // As on scene unload; the views stay, so only the playback's own cleanup can stop their tweens.
            Object.DestroyImmediate(playback);

            Assert.That(Tween.GetTweensCount(falling), Is.Zero, "No callback can outlive the playback.");
        }

        [Test]
        public void PlayingAMove_AllocatesNoMemory_AfterWarmUp()
        {
            // A 6 × 6 board filled from a seed, which the domain guarantees has a valid chain.
            session = PlaySessions.StartRandom(6, 6, new Pcg32(42), moves: 1000);
            boardView.Init(session.Board, catalog);
            playback.Init(boardView, 6, 6);

            for (int i = 0; i < 5; i++) PlayOneMove();

            Assert.That(() => PlayOneMove(), Is.Not.AllocatingGCMemory());
        }

        // Finds any two neighbouring spores of one type, commits them, and plays the move to its end at once.
        void PlayOneMove()
        {
            IReadOnlyBoard board = session.Board;
            for (int column = 0; column < board.Width; column++)
            for (int row = 0; row < board.Height; row++)
            for (int dc = -1; dc <= 1; dc++)
            for (int dr = -1; dr <= 1; dr++)
            {
                var from = new GridPosition(column, row);
                var to = new GridPosition(column + dc, row + dr);
                if ((dc == 0 && dr == 0) || !board.IsInside(to) || board.GetOccupant(from) != board.GetOccupant(to))
                    continue;

                session.Chain.Touch(from);
                session.Chain.Touch(to);
                playback.Play(session.Commit());
                playback.CompleteNow();
                return;
            }

            Assert.Fail("No pair to chain.");
        }
    }
}
