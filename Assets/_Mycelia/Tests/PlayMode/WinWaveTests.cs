using System.Collections;
using System.Collections.Generic;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mycelia.Game.PlayTests
{
    // Soni (2026-10-25): a win shows itself on the board first, the spores popping in a wave out from the last chain,
    // then the Level Completed screen; a tap skips to it.
    public class WinWaveTests
    {
        const string Grid = @"
            A B C D
            B C D A
            C D A B
            D A B C";

        GameObject root;
        GameObject sporeTemplate;
        BoardView boardView;
        WinWave wave;
        Button skip;
        RecordingListener next;

        sealed class RecordingListener : IPlaybackListener
        {
            public readonly List<string> Events = new List<string>();
            public void MovesChanged(int remaining) => Events.Add($"moves {remaining}");
            public void ObjectiveProgressed(int index, int current, int target) => Events.Add($"goal {index}: {current}/{target}");
            public void SessionEnded(SessionState outcome, int stars) => Events.Add($"ended {outcome} {stars}");
        }

        static GridPosition At(int column, int row) => new GridPosition(column, row);

        [SetUp]
        public void CreateViews()
        {
            root = new GameObject("Board");
            sporeTemplate = new GameObject("Spore Template", typeof(SpriteRenderer));
            var sporeView = sporeTemplate.AddComponent<SporeView>();
            SetReference(sporeView, "spriteRenderer", sporeTemplate.GetComponent<SpriteRenderer>());
            sporeTemplate.SetActive(false);
            boardView = root.AddComponent<BoardView>();
            SetReference(boardView, "sporePrefab", sporeView);
            LevelSession session = PlaySessions.Start(Grid, new ScriptedRandom(1));
            boardView.Init(session.Board, PlaySessions.ProjectSpores);

            skip = new GameObject("Skip", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            skip.transform.SetParent(root.transform);
            wave = root.AddComponent<WinWave>();
            SetReference(wave, "skip", skip);
            next = new RecordingListener();
            // The last chain ended in the bottom-left corner.
            wave.Init(boardView, 4, 4, () => At(0, 0), next);
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
        }

        float Scale(int column, int row) => boardView.SporeAt(At(column, row)).transform.localScale.x;

        IEnumerator WaitWhilePlaying()
        {
            float giveUp = Time.realtimeSinceStartup + 5f;
            while (wave.IsPlaying && Time.realtimeSinceStartup < giveUp)
                yield return null;
        }

        [Test]
        public void MovesAndGoals_PassStraightThrough()
        {
            wave.MovesChanged(7);
            wave.ObjectiveProgressed(0, 3, 12);

            Assert.That(next.Events, Is.EqualTo(new[] { "moves 7", "goal 0: 3/12" }));
        }

        [Test]
        public void ALoss_ShowsTheResultAtOnce_WithoutAWave()
        {
            wave.SessionEnded(SessionState.Lost, 0);

            Assert.That(next.Events, Is.EqualTo(new[] { "ended Lost 0" }));
            Assert.That(wave.IsPlaying, Is.False);
            Assert.That(skip.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator AWin_HoldsTheResult_UntilTheWaveHasPlayed()
        {
            wave.SessionEnded(SessionState.Won, 2);
            Assert.That(next.Events, Is.Empty, "The result waits for the wave.");
            Assert.That(wave.IsPlaying, Is.True);

            yield return WaitWhilePlaying();

            Assert.That(next.Events, Is.EqualTo(new[] { "ended Won 2" }));
            for (int column = 0; column < 4; column++)
            for (int row = 0; row < 4; row++)
                Assert.That(Scale(column, row), Is.EqualTo(1f).Within(1e-4f), $"({column}, {row}) back to size");
        }

        [UnityTest]
        public IEnumerator TheWave_StartsWhereTheLastChainEnded_AndSpreadsOut()
        {
            wave.SessionEnded(SessionState.Won, 1);

            yield return new WaitForSeconds(0.1f);

            Assert.That(Scale(0, 0), Is.GreaterThan(1f), "The spore where the chain ended pops first.");
            Assert.That(Scale(3, 3), Is.EqualTo(1f), "The far corner hasn't been reached yet.");

            float giveUp = Time.realtimeSinceStartup + 3f;
            while (Scale(3, 3) <= 1f && wave.IsPlaying && Time.realtimeSinceStartup < giveUp)
                yield return null;
            Assert.That(Scale(3, 3), Is.GreaterThan(1f), "Then the wave reaches it.");
        }

        [UnityTest]
        public IEnumerator Skipping_ShowsTheResultAtOnce_AndOnlyOnce()
        {
            wave.SessionEnded(SessionState.Won, 3);
            yield return null;

            wave.Skip();

            Assert.That(next.Events, Is.EqualTo(new[] { "ended Won 3" }));
            Assert.That(wave.IsPlaying, Is.False);
            Assert.That(Scale(0, 0), Is.EqualTo(1f).Within(1e-4f), "Spores snap back to size.");

            wave.Skip();
            yield return new WaitForSeconds(1.5f);
            Assert.That(next.Events, Is.EqualTo(new[] { "ended Won 3" }), "Still once.");
        }

        [UnityTest]
        public IEnumerator TheSkipButton_CoversTheScreenOnlyWhileTheWavePlays_AndATapSkips()
        {
            Assert.That(skip.gameObject.activeSelf, Is.False);

            wave.SessionEnded(SessionState.Won, 1);
            Assert.That(skip.gameObject.activeSelf, Is.True);
            yield return null;
            skip.onClick.Invoke();

            Assert.That(next.Events, Is.EqualTo(new[] { "ended Won 1" }));
            Assert.That(skip.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyingTheBoardMidWave_StopsItsTweens()
        {
            wave.SessionEnded(SessionState.Won, 1);
            yield return null;

            Object.DestroyImmediate(root);
            yield return new WaitForSeconds(1.5f);

            Assert.That(next.Events, Is.Empty, "No callback reaches a destroyed scene (ADR-0009 rule 5).");
        }
    }
}
