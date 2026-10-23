using Mycelia.Domain.Sessions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    public class ResultPanelTests
    {
        TestHud hud;
        int playAgainClicks;

        [SetUp]
        public void CreatePanel()
        {
            hud = new TestHud();
            hud.Panel.Init(() => playAgainClicks++);
        }

        [TearDown]
        public void DestroyPanel() => hud.Destroy();

        string Title => hud.Panel.GetComponentInChildren<TMP_Text>(true).text;

        Image[] Stars => System.Array.FindAll(hud.Panel.GetComponentsInChildren<Image>(true), image => image.name.StartsWith("Star"));

        [Test]
        public void ThePanel_StartsHidden()
        {
            Assert.That(hud.Panel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void AWin_ShowsItsStars_EarnedOnesLit()
        {
            hud.Panel.Show(SessionState.Won, 2);

            Assert.That(hud.Panel.gameObject.activeSelf, Is.True);
            Assert.That(Title, Is.EqualTo("Level complete"));
            Assert.That(System.Array.TrueForAll(Stars, star => star.gameObject.activeSelf), "All three stars are shown.");
            Assert.That(Stars[0].color, Is.EqualTo(hud.Panel.EarnedStarColour));
            Assert.That(Stars[1].color, Is.EqualTo(hud.Panel.EarnedStarColour));
            Assert.That(Stars[2].color, Is.EqualTo(hud.Panel.MissingStarColour));
        }

        [Test]
        public void ALoss_SaysSo_WithoutStars()
        {
            hud.Panel.Show(SessionState.Lost, 0);

            Assert.That(Title, Is.EqualTo("Out of moves"));
            Assert.That(System.Array.TrueForAll(Stars, star => !star.gameObject.activeSelf), "No stars on a loss.");
        }

        [Test]
        public void PlayAgain_AsksToRestartTheLevel()
        {
            hud.Panel.Show(SessionState.Lost, 0);

            hud.Panel.GetComponentInChildren<Button>(true).onClick.Invoke();

            Assert.That(playAgainClicks, Is.EqualTo(1));
        }
    }
}
