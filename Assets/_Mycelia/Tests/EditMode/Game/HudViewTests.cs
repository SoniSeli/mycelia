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
            hud.Hud.Init(config, catalog);
        }

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

        [Test]
        public void Goals_SitSideBySide_CentredInTheHud()
        {
            Vector2 first = ((RectTransform)Slots[0].transform).anchoredPosition;
            Vector2 second = ((RectTransform)Slots[1].transform).anchoredPosition;

            Assert.That(first.x, Is.EqualTo(-second.x).Within(1e-3f));
            Assert.That(second.x - first.x, Is.GreaterThanOrEqualTo(200f), "Slots don't overlap.");
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
