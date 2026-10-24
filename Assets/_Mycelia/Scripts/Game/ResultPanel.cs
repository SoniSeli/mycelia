using System;
using Mycelia.Domain.Sessions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>A level's fact card (design §3.6): text, an optional image, and the image's credit.</summary>
    public readonly struct FactCard
    {
        public readonly string Text;
        public readonly Sprite Image;
        public readonly string Credit;

        public FactCard(string text, Sprite image, string credit)
        {
            Text = text;
            Image = image;
            Credit = credit;
        }
    }

    /// <summary>
    /// Shows how the level ended (design §3.4–3.6, §4). A win shows 1–3 stars and the level's fact card, with Level
    /// select so the player sees the new unlock and their stars. A loss says "Out of moves", with Retry and Level
    /// select. The card's height follows what it holds; positions are set here, not by a layout group (ADR-0012).
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        public const string WonTitle = "Level complete";
        public const string LostTitle = "Out of moves";

        [SerializeField] TMP_Text title;
        [SerializeField] Image[] stars = Array.Empty<Image>();
        [SerializeField, FormerlySerializedAs("playAgain")] Button retry;
        [SerializeField] Button levelSelect;
        [SerializeField, Tooltip("The visible panel; its height is set per outcome. Children are anchored to its top or bottom.")]
        RectTransform card;
        [SerializeField] GameObject fact;
        [SerializeField] Image factImage;
        [SerializeField] TMP_Text factText;
        [SerializeField] TMP_Text factCredit;
        [SerializeField] Color earnedStarColour = new Color(1f, 0.8f, 0.25f);
        [SerializeField] Color missingStarColour = new Color(1f, 1f, 1f, 0.2f);

        [Header("Card heights")]
        [SerializeField, Min(0)] float lossHeight = 640;
        [SerializeField, Min(0)] float winHeight = 1030;
        [SerializeField, Min(0), Tooltip("Added to a win's card when the fact has an image; the text and credit move down by as much.")]
        float imageHeight = 380;

        FactCard factCard;
        Vector2 factTextTop;
        Vector2 factCreditTop;
        float factTextHeight;
        bool measured;

        public Color EarnedStarColour => earnedStarColour;
        public Color MissingStarColour => missingStarColour;

        public void Init(Action onRetry, Action onLevelSelect, FactCard fact)
        {
            retry.onClick.RemoveAllListeners();
            retry.onClick.AddListener(() => onRetry());
            levelSelect.onClick.RemoveAllListeners();
            levelSelect.onClick.AddListener(() => onLevelSelect());
            factCard = fact;
            Hide();
        }

        public void Hide() => gameObject.SetActive(false);

        public void Show(SessionState outcome, int earnedStars)
        {
            bool won = outcome == SessionState.Won;
            title.SetText(won ? WonTitle : LostTitle);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(won);
                stars[i].color = i < earnedStars ? earnedStarColour : missingStarColour;
            }

            retry.gameObject.SetActive(!won);
            levelSelect.gameObject.SetActive(true);
            float height = won ? winHeight + ShowFact() : lossHeight;
            if (!won)
                fact.SetActive(false);
            card.sizeDelta = new Vector2(card.sizeDelta.x, height);
            gameObject.SetActive(true);
        }

        // Fills the fact card and returns the extra height its image and its text need beyond the scene's layout.
        // The text box grows to fit the whole fact (never cut), and the credit moves down below it.
        float ShowFact()
        {
            if (!measured)
            {
                factTextTop = factText.rectTransform.anchoredPosition;
                factCreditTop = factCredit.rectTransform.anchoredPosition;
                factTextHeight = factText.rectTransform.sizeDelta.y;
                measured = true;
            }

            bool hasImage = factCard.Image != null;
            // The credit belongs to the image: without the image, it isn't shown.
            bool hasCredit = hasImage && !string.IsNullOrEmpty(factCard.Credit);
            float imageExtra = hasImage ? imageHeight : 0;

            fact.SetActive(!string.IsNullOrEmpty(factCard.Text) || hasImage);
            factImage.gameObject.SetActive(hasImage);
            if (hasImage)
                factImage.sprite = factCard.Image;

            RectTransform textRect = factText.rectTransform;
            factText.text = factCard.Text ?? "";
            float needed = factText.GetPreferredValues(factText.text, textRect.rect.width, 0).y;
            float textHeight = Mathf.Max(factTextHeight, Mathf.Ceil(needed));
            float textExtra = textHeight - factTextHeight;
            textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, textHeight);
            textRect.anchoredPosition = factTextTop - new Vector2(0, imageExtra);

            factCredit.gameObject.SetActive(hasCredit);
            factCredit.text = factCard.Credit ?? "";
            factCredit.rectTransform.anchoredPosition = factCreditTop - new Vector2(0, imageExtra + textExtra);
            return imageExtra + textExtra;
        }
    }
}
