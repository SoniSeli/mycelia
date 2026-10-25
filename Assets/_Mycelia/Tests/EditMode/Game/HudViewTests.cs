using System.Linq;
using Mycelia.Content;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.Tests
{
    public class HudViewTests
    {
        TestHud hud;
        SporeCatalog catalog;
        LevelConfig config;

        [SetUp]
        public void CreateHud()
        {
            hud = new TestHud();
            catalog = TestViews.ProjectSpores;
            // 20 moves; collect 12 of type A (id 1) and 8 of type B (id 2).
            config = new LevelConfig("level-hud", 6, 6, 20,
                "ABCD".Select(TestSessions.IdOf).ToArray(),
                new[] { new ObjectiveDefinition(TestSessions.IdOf('A'), 12), new ObjectiveDefinition(TestSessions.IdOf('B'), 8) });
            pauses = 0;
            hud.Hud.Init(config, 9, catalog, () => pauses++);
        }

        int pauses;

        [TearDown]
        public void DestroyHud() => hud.Destroy();

        ObjectiveSlot[] Slots => hud.Slots.GetComponentsInChildren<ObjectiveSlot>(includeInactive: true);

        static string Count(ObjectiveSlot slot) => slot.GetComponentInChildren<TMP_Text>(true).text;

        [Test]
        public void AtTheStart_TheHudShowsEveryMove_AndEachGoalFromZero()
        {
            Assert.That(hud.Moves.text, Is.EqualTo("20"));
            Assert.That(Slots.Length, Is.EqualTo(2));
            Assert.That(Count(Slots[0]), Is.EqualTo("0/12"));
            Assert.That(Count(Slots[1]), Is.EqualTo("0/8"));
            Assert.That(Slots.All(slot => slot.gameObject.activeSelf));
        }

        [Test]
        public void EachGoal_ShowsItsSporesSilhouette_InItsColour()
        {
            var icon = Slots[1].GetComponentInChildren<UnityEngine.UI.Image>(true);
            SporeType type = catalog.TypeOf(TestSessions.IdOf('B'));

            Assert.That(icon.sprite, Is.SameAs(type.Sprite));
            Assert.That(icon.color, Is.EqualTo(type.Color));
        }

        // Design §8.3: the goals share the card with Moves; each gets an equal share of the goal area, so up to four fit
        // whatever the screen's width.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void Goals_ShareTheirAreaEvenly_SideBySide(int count)
        {
            hud.Destroy();
            hud = new TestHud();
            var goals = Enumerable.Range(0, count).Select(i => new ObjectiveDefinition(TestSessions.IdOf("ABCD"[i]), 10)).ToArray();
            hud.Hud.Init(new LevelConfig("level-hud", 6, 6, 20, "ABCD".Select(TestSessions.IdOf).ToArray(), goals), 1, catalog, () => { });

            for (int i = 0; i < count; i++)
            {
                var rect = (RectTransform)Slots[i].transform;
                float share = 1f / count;
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(i * share, 0)), $"goal {i + 1}");
                Assert.That(rect.anchorMax, Is.EqualTo(new Vector2((i + 1) * share, 1)), $"goal {i + 1}");
                Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero), $"goal {i + 1}");
                Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero), $"goal {i + 1}");
            }
        }

        // Decided 2026-10-25 (slice 4): below the board, centred in the band the board leaves free there.
        [Test]
        public void ThePauseButton_SitsCentredInTheBandBelowTheBoard()
        {
            var rect = (RectTransform)hud.PauseButton.transform;
            var centre = new Vector2(0.5f, hud.Layout.Insets.Bottom / 2);

            Assert.That(rect.anchorMin, Is.EqualTo(centre));
            Assert.That(rect.anchorMax, Is.EqualTo(centre));
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TheTitle_NamesTheLevelByItsNumber_OrJustLevelWithoutOne()
        {
            Assert.That(hud.Title.text, Is.EqualTo("Level 9"));

            hud.Destroy();
            hud = new TestHud();
            hud.Hud.Init(config, 0, catalog, () => { });
            Assert.That(hud.Title.text, Is.EqualTo("Level"));
        }

        [Test]
        public void ThePauseButton_AsksToPauseTheLevel()
        {
            hud.PauseButton.onClick.Invoke();

            Assert.That(pauses, Is.EqualTo(1));
        }

        [Test]
        public void TheHud_FillsTheTopBandTheBoardLeavesFree()
        {
            BoardInsets insets = hud.Layout.Insets;

            Assert.That(hud.Band.anchorMin, Is.EqualTo(new Vector2(insets.Side, 1 - insets.Top)));
            Assert.That(hud.Band.anchorMax, Is.EqualTo(new Vector2(1 - insets.Side, 1)));
        }

        [Test]
        public void MovesAndGoals_UpdateWhenPlaybackReportsThem()
        {
            hud.Hud.MovesChanged(19);
            hud.Hud.ObjectiveProgressed(1, 3, 8);

            Assert.That(hud.Moves.text, Is.EqualTo("19"));
            Assert.That(Count(Slots[0]), Is.EqualTo("0/12"));
            Assert.That(Count(Slots[1]), Is.EqualTo("3/8"));
        }

        [Test]
        public void TheEndOfTheLevel_OpensTheResultPanel()
        {
            Assert.That(hud.Panel.gameObject.activeSelf, Is.False);

            hud.Hud.SessionEnded(SessionState.Won, 2);

            Assert.That(hud.Panel.gameObject.activeSelf, Is.True);
        }

        // TextMeshPro's numeric SetText allocates only in the Editor, where it rebuilds the string for the Inspector
        // (TMP_Text.SetText, #if UNITY_EDITOR); players don't, so the counters themselves are checked on device (slice 8).
        // This guards the part that is ours: the pulse on a goal that changed.
        [Test]
        public void PulsingAGoal_AllocatesNoMemory_AfterWarmUp()
        {
            ObjectiveSlot slot = Slots[0];
            for (int i = 0; i < 3; i++) slot.Pulse();

            Assert.That(() => slot.Pulse(), Is.Not.AllocatingGCMemory());
        }
    }
}
