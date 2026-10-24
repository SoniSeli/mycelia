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
        Texture2D texture;
        Sprite factSprite;
        int retryClicks;
        int levelSelectClicks;

        [SetUp]
        public void CreatePanel()
        {
            hud = new TestHud();
            texture = new Texture2D(4, 4);
            factSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            retryClicks = levelSelectClicks = 0;
            Init(new FactCard("Mycelium can span kilometres.", null, ""));
        }

        [TearDown]
        public void DestroyPanel()
        {
            hud.Destroy();
            Object.DestroyImmediate(factSprite);
            Object.DestroyImmediate(texture);
        }

        void Init(FactCard fact) => hud.Panel.Init(() => retryClicks++, () => levelSelectClicks++, fact);

        Transform Find(string name)
        {
            foreach (Transform child in hud.Panel.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        string Title => Find("Title").GetComponent<TMP_Text>().text;
        bool Shown(string name) => Find(name).gameObject.activeSelf;
        float CardHeight => ((RectTransform)Find("Card")).sizeDelta.y;

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
        public void AWin_ShowsTheLevelsFact_AndOnlyLevelSelect()
        {
            // Slice 5: every win returns to level select, so the player sees the new unlock and their stars.
            hud.Panel.Show(SessionState.Won, 3);

            Assert.That(Shown("Fact"), Is.True);
            Assert.That(Find("Fact Text").GetComponent<TMP_Text>().text, Is.EqualTo("Mycelium can span kilometres."));
            Assert.That(Shown("Level Select"), Is.True);
            Assert.That(Shown("Retry"), Is.False);
        }

        [Test]
        public void AFactWithoutAnImageOrCredit_HidesThem()
        {
            hud.Panel.Show(SessionState.Won, 1);

            Assert.That(Shown("Fact Image"), Is.False);
            Assert.That(Shown("Fact Credit"), Is.False);
        }

        [Test]
        public void ACreditWithoutItsImage_IsHidden()
        {
            // The credit names who made the image; a build without the (local-only) photo shows neither.
            Init(new FactCard("Text.", null, "Photo: example.org"));

            hud.Panel.Show(SessionState.Won, 1);

            Assert.That(Shown("Fact Credit"), Is.False);
        }

        [Test]
        public void AFactWithAnImageAndCredit_ShowsThem_OnATallerCard()
        {
            hud.Panel.Show(SessionState.Won, 1);
            float withoutImage = CardHeight;

            Init(new FactCard("Text.", factSprite, "Photo: Jane Doe, CC BY 4.0"));
            hud.Panel.Show(SessionState.Won, 1);

            Assert.That(Shown("Fact Image"), Is.True);
            Assert.That(Find("Fact Image").GetComponent<Image>().sprite, Is.SameAs(factSprite));
            Assert.That(Shown("Fact Credit"), Is.True);
            Assert.That(Find("Fact Credit").GetComponent<TMP_Text>().text, Is.EqualTo("Photo: Jane Doe, CC BY 4.0"));
            Assert.That(CardHeight, Is.GreaterThan(withoutImage));
        }

        [Test]
        public void ALongFact_GrowsTheCard_AndMovesTheCreditBelowIt()
        {
            // Slice 7: facts keep their full text, so the card grows to fit it instead of cutting it.
            float box = ((RectTransform)Find("Fact Text")).sizeDelta.y;
            Init(new FactCard("Short.", null, "Credit"));
            hud.Panel.Show(SessionState.Won, 1);
            float shortCard = CardHeight;
            float shortCredit = ((RectTransform)Find("Fact Credit")).anchoredPosition.y;

            Init(new FactCard(string.Join(" ", System.Linq.Enumerable.Repeat("Mycelium grows through soil and wood.", 12)), null, "Credit"));
            hud.Panel.Show(SessionState.Won, 1);

            Assert.That(CardHeight, Is.GreaterThan(shortCard));
            Assert.That(((RectTransform)Find("Fact Credit")).anchoredPosition.y, Is.LessThan(shortCredit), "The credit sits lower, under the longer text.");
            float grown = CardHeight - shortCard;
            float textGrew = ((RectTransform)Find("Fact Text")).sizeDelta.y - box;
            Assert.That(grown, Is.EqualTo(textGrew).Within(0.01f), "The card grows by exactly what the text needs.");
        }

        [Test]
        public void ALoss_SaysSo_WithRetryAndLevelSelect_ButNoStarsOrFact()
        {
            hud.Panel.Show(SessionState.Lost, 0);

            Assert.That(Title, Is.EqualTo("Out of moves"));
            Assert.That(System.Array.TrueForAll(Stars, star => !star.gameObject.activeSelf), "No stars on a loss.");
            Assert.That(Shown("Fact"), Is.False);
            Assert.That(Shown("Retry"), Is.True);
            Assert.That(Shown("Level Select"), Is.True);
        }

        [Test]
        public void ALoss_UsesTheShortCard()
        {
            hud.Panel.Show(SessionState.Won, 1);
            float win = CardHeight;

            hud.Panel.Show(SessionState.Lost, 0);

            Assert.That(CardHeight, Is.LessThan(win));
        }

        [Test]
        public void Retry_AsksToRestartTheLevel()
        {
            hud.Panel.Show(SessionState.Lost, 0);

            Find("Retry").GetComponent<Button>().onClick.Invoke();

            Assert.That(retryClicks, Is.EqualTo(1));
            Assert.That(levelSelectClicks, Is.Zero);
        }

        [Test]
        public void LevelSelect_AsksToLeaveTheLevel()
        {
            hud.Panel.Show(SessionState.Won, 2);

            Find("Level Select").GetComponent<Button>().onClick.Invoke();

            Assert.That(levelSelectClicks, Is.EqualTo(1));
            Assert.That(retryClicks, Is.Zero);
        }
    }
}
