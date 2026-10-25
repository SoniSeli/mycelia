using System.Linq;
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
            // A photo twice as wide as it is tall.
            texture = new Texture2D(8, 4);
            factSprite = Sprite.Create(texture, new Rect(0, 0, 8, 4), Vector2.one * 0.5f);
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

        RectTransform Rect(string name) => (RectTransform)Find(name);
        string Title => Find("Title").GetComponent<TMP_Text>().text;
        bool Shown(string name) => Find(name).gameObject.activeSelf;

        Image[] Stars => System.Array.FindAll(hud.Panel.GetComponentsInChildren<Image>(true), image => image.name.StartsWith("Star"));

        static string LongFact(int sentences) => string.Join(" ", Enumerable.Repeat("Mycelium grows through soil and wood.", sentences));

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
            Assert.That(Title, Is.EqualTo("Level Completed"));
            Assert.That(System.Array.TrueForAll(Stars, star => star.gameObject.activeSelf), "All three stars are shown.");
            Assert.That(Stars[0].color, Is.EqualTo(hud.Panel.EarnedStarColour));
            Assert.That(Stars[1].color, Is.EqualTo(hud.Panel.EarnedStarColour));
            Assert.That(Stars[2].color, Is.EqualTo(hud.Panel.MissingStarColour));
        }

        [Test]
        public void AWin_ShowsTheLevelsFact_AndOnlyContinue()
        {
            // Slice 5, kept in Milestone 4 slice 7 (Soni): every win returns to level select, so the player sees the new
            // unlock and their stars.
            hud.Panel.Show(SessionState.Won, 3);

            Assert.That(Shown("Fact"), Is.True);
            Assert.That(Find("Fact Text").GetComponent<TMP_Text>().text, Is.EqualTo("Mycelium can span kilometres."));
            Assert.That(Shown("Level Select"), Is.True);
            Assert.That(Shown("Retry"), Is.False);
            Assert.That(Rect("Level Select").anchoredPosition.x, Is.EqualTo(0f), "Continue sits centred.");
        }

        // Design §8.3 slice 7: a win fills the screen over the backdrop, with nothing of the level showing through.
        [Test]
        public void AWin_FillsTheScreen_HidingTheBoardAndHud()
        {
            hud.Panel.Show(SessionState.Won, 1);

            Assert.That(Rect("Card").rect.size, Is.EqualTo(((RectTransform)hud.Panel.transform).rect.size), "The card fills the panel.");
            Assert.That(Find("Card").GetComponent<Image>().enabled, Is.False, "No card shape: the backdrop shows.");
            Assert.That(Shown("Dim"), Is.False);
            Assert.That(hud.Board.activeSelf, Is.False);
            Assert.That(hud.Band.gameObject.activeSelf, Is.False);
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

        // Design §8.3 slice 7: text, then the photo at full width in its own shape, then its source under its bottom-left corner.
        [Test]
        public void AFactsPhoto_KeepsItsShape_UnderTheText_WithItsSourceUnderItsLeftEdge()
        {
            Init(new FactCard("Text.", factSprite, "Photo: example.org"));

            hud.Panel.Show(SessionState.Won, 1);

            RectTransform photo = Rect("Fact Image"), text = Rect("Fact Text"), credit = Rect("Fact Credit");
            Assert.That(Shown("Fact Image"), Is.True);
            Assert.That(Find("Fact Image").GetComponent<Image>().sprite, Is.SameAs(factSprite));
            Assert.That(photo.sizeDelta, Is.EqualTo(new Vector2(940, 470)), "As wide as the text, twice as wide as tall.");
            Assert.That(photo.anchoredPosition.y, Is.LessThan(text.anchoredPosition.y - text.sizeDelta.y), "Under the text.");
            Assert.That(Shown("Fact Credit"), Is.True);
            Assert.That(Find("Fact Credit").GetComponent<TMP_Text>().text, Is.EqualTo("Photo: example.org"));
            Assert.That(credit.anchoredPosition.x - credit.pivot.x * credit.sizeDelta.x, Is.EqualTo(-470f).Within(0.01f), "Starts at the photo's left edge.");
            Assert.That(credit.anchoredPosition.y, Is.LessThanOrEqualTo(photo.anchoredPosition.y - photo.sizeDelta.y), "Under the photo.");
        }

        float Setting(string name) => new UnityEditor.SerializedObject(hud.Panel).FindProperty(name).floatValue;
        static float Bottom(RectTransform rect) => rect.anchoredPosition.y - rect.sizeDelta.y;
        float StarsBottom => Bottom(Rect("Star 3"));
        float ContinueTop => -Rect("Card").rect.height + Rect("Level Select").anchoredPosition.y + Rect("Level Select").sizeDelta.y;

        // Soni (slice 7): on a tall phone the fact sits a little above centre between the stars and Continue, with room
        // between its parts; Continue rises off the bottom edge and the title and stars come down from the top.
        [Test]
        public void OnATallScreen_TheFactSitsAboveCentre_WithItsSpacesAtTheirMost_ContinueRises_AndTheTitleComesDown()
        {
            Init(new FactCard("Text.", factSprite, "Photo: example.org"));

            hud.Panel.Show(SessionState.Won, 1);

            RectTransform heading = Rect("Interesting Fact"), text = Rect("Fact Text"), photo = Rect("Fact Image"), credit = Rect("Fact Credit");
            Assert.That(Rect("Level Select").anchoredPosition.y, Is.GreaterThan(40f), "Continue rises from where it sits on a short screen.");
            Assert.That(Rect("Title").anchoredPosition.y, Is.LessThan(-60f), "The title comes down from where it sits on a short screen.");
            Assert.That(Rect("Star 2").anchoredPosition.y - Rect("Title").anchoredPosition.y, Is.EqualTo(-140f).Within(0.01f), "The stars come down with it.");
            Assert.That(StarsBottom - heading.anchoredPosition.y, Is.LessThan(Bottom(credit) - ContinueTop), "Less room above the fact than below it.");
            Assert.That(StarsBottom - heading.anchoredPosition.y, Is.GreaterThan(Setting("gap")), "But more than the least.");
            Assert.That(Bottom(heading) - text.anchoredPosition.y, Is.EqualTo(Setting("maxGap")).Within(0.01f), "Heading to text.");
            Assert.That(Bottom(text) - photo.anchoredPosition.y, Is.EqualTo(Setting("maxGap")).Within(0.01f), "Text to photo.");
        }

        // Soni (slice 7): on a short (16:9) phone the parts stay close, and the photo gives way.
        [Test]
        public void OnAShortScreen_TheSpacesStayAtTheirLeast_AndThePhotoShrinksToFit()
        {
            ((RectTransform)hud.Panel.transform).sizeDelta = new Vector2(1080, 1700);
            Init(new FactCard(LongFact(12), factSprite, "Photo: example.org"));

            hud.Panel.Show(SessionState.Won, 1);

            RectTransform heading = Rect("Interesting Fact"), text = Rect("Fact Text"), photo = Rect("Fact Image"), credit = Rect("Fact Credit");
            Assert.That(photo.sizeDelta.y, Is.LessThan(470f), "Shrunk from its full width.");
            Assert.That(Bottom(heading) - text.anchoredPosition.y, Is.EqualTo(Setting("gap")).Within(0.01f), "Heading to text.");
            Assert.That(Bottom(text) - photo.anchoredPosition.y, Is.EqualTo(Setting("gap")).Within(0.01f), "Text to photo.");
            Assert.That(StarsBottom - heading.anchoredPosition.y, Is.EqualTo(Setting("gap")).Within(0.5f), "Right under the stars.");
            Assert.That(Bottom(credit) - ContinueTop, Is.EqualTo(Setting("gap")).Within(0.5f), "Right above Continue.");
            Assert.That(Rect("Level Select").anchoredPosition.y, Is.EqualTo(40f), "Continue stays where it is.");
            Assert.That(Rect("Title").anchoredPosition.y, Is.EqualTo(-60f), "So do the title...");
            Assert.That(Rect("Star 2").anchoredPosition.y, Is.EqualTo(-200f), "...and the stars.");
        }

        [Test]
        public void AFactWithoutAPhoto_SitsAboveCentreToo()
        {
            hud.Panel.Show(SessionState.Won, 1);

            RectTransform heading = Rect("Interesting Fact"), text = Rect("Fact Text");
            Assert.That(StarsBottom - heading.anchoredPosition.y, Is.LessThan(Bottom(text) - ContinueTop));
            Assert.That(StarsBottom - heading.anchoredPosition.y, Is.GreaterThan(Setting("gap")));
        }

        // Facts keep their full text (Milestone 3 slice 7); on a full screen the photo gives way instead.
        [Test]
        public void ALongFact_KeepsAllItsText_AndThePhotoShrinksToFitAboveContinue()
        {
            Init(new FactCard(LongFact(30), factSprite, "Credit"));

            hud.Panel.Show(SessionState.Won, 1);

            RectTransform photo = Rect("Fact Image"), text = Rect("Fact Text"), credit = Rect("Fact Credit");
            TMP_Text factText = text.GetComponent<TMP_Text>();
            float needed = factText.GetPreferredValues(factText.text, text.rect.width, 0).y;
            Assert.That(text.sizeDelta.y, Is.GreaterThanOrEqualTo(needed), "The whole fact fits its box.");
            Assert.That(Shown("Fact Image"), Is.True);
            Assert.That(photo.sizeDelta.y, Is.LessThan(470f), "Smaller than it would be with a short fact.");
            Assert.That(photo.sizeDelta.x / photo.sizeDelta.y, Is.EqualTo(2f).Within(0.01f), "Still in its own shape.");
            float cardHeight = Rect("Card").rect.height;
            float creditBottomFromCardBottom = cardHeight + credit.anchoredPosition.y - credit.sizeDelta.y;
            RectTransform continueButton = Rect("Level Select");
            Assert.That(creditBottomFromCardBottom, Is.GreaterThanOrEqualTo(continueButton.anchoredPosition.y + continueButton.sizeDelta.y),
                "The source stays above Continue.");
        }

        [Test]
        public void APhotoThatWouldFallBelowItsMinimumHeight_IsDroppedWithItsSource_RatherThanSqueezed()
        {
            // The photo would be 470 tall here: just under a minimum of 480.
            var serialized = new UnityEditor.SerializedObject(hud.Panel);
            serialized.FindProperty("minPhotoHeight").floatValue = 480;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Init(new FactCard("Text.", factSprite, "Credit"));

            hud.Panel.Show(SessionState.Won, 1);

            Assert.That(Shown("Fact Image"), Is.False);
            Assert.That(Shown("Fact Credit"), Is.False);
        }

        [Test]
        public void ALoss_SaysSo_WithThreeDimmedStars_RetryAndContinue_ButNoFact()
        {
            hud.Panel.Show(SessionState.Lost, 0);

            Assert.That(Title, Is.EqualTo("Out of moves"));
            Assert.That(Stars.All(star => star.gameObject.activeSelf && star.color == hud.Panel.MissingStarColour), "Three dimmed stars.");
            Assert.That(Shown("Fact"), Is.False);
            Assert.That(Shown("Retry"), Is.True);
            Assert.That(Shown("Level Select"), Is.True);
            Assert.That(Rect("Retry").anchoredPosition.x, Is.LessThan(0f), "Retry on the left.");
            Assert.That(Rect("Level Select").anchoredPosition.x, Is.EqualTo(-Rect("Retry").anchoredPosition.x), "Continue on the right, side by side.");
            Assert.That(Rect("Retry").anchoredPosition.y, Is.EqualTo(Rect("Level Select").anchoredPosition.y));
        }

        // Design §8.3 slice 7 (Soni): a loss is a small card over the dimmed board.
        [Test]
        public void ALoss_IsASmallCardOverTheDimmedBoard()
        {
            hud.Panel.Show(SessionState.Won, 1);
            hud.Panel.Show(SessionState.Lost, 0);

            Vector2 panel = ((RectTransform)hud.Panel.transform).rect.size;
            Assert.That(Rect("Card").rect.width, Is.LessThan(panel.x));
            Assert.That(Rect("Card").rect.height, Is.LessThan(panel.y / 2));
            Assert.That(Find("Card").GetComponent<Image>().enabled, Is.True);
            Assert.That(Shown("Dim"), Is.True);
            Assert.That(hud.Board.activeSelf, Is.True, "The board stays in view.");
            Assert.That(hud.Band.gameObject.activeSelf, Is.True);
            Assert.That(Rect("Title").anchoredPosition.y, Is.EqualTo(-60f), "The title is back in its place on the card.");
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
        public void Continue_AsksToLeaveForLevelSelect()
        {
            hud.Panel.Show(SessionState.Won, 2);

            Find("Level Select").GetComponent<Button>().onClick.Invoke();

            Assert.That(levelSelectClicks, Is.EqualTo(1));
            Assert.That(retryClicks, Is.Zero);
        }
    }
}
