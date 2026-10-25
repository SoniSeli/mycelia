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
    /// Shows how the level ended (design §3.4–3.6, §4, §8.3). A win fills the screen over the backdrop, with the board and
    /// the HUD hidden: "Level Completed", 1–3 stars, the level's fact under "Interesting Fact", its photo and source, and
    /// Continue to level select, so the player sees the new unlock and their stars. A loss is a small card over the dimmed
    /// board: "Out of moves", three dimmed stars, Retry and Continue. Positions are set here, not by a layout group (ADR-0012).
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        public const string WonTitle = "Level Completed";
        public const string LostTitle = "Out of moves";

        [SerializeField] TMP_Text title;
        [SerializeField] Image[] stars = Array.Empty<Image>();
        [SerializeField, FormerlySerializedAs("playAgain")] Button retry;
        [SerializeField, Tooltip("Continue: back to level select.")] Button levelSelect;
        [SerializeField, Tooltip("Holds everything shown: the whole panel for a win, a small card for a loss. Children are anchored to its top or bottom.")]
        RectTransform card;
        [SerializeField, Tooltip("The card's shape, drawn only for a loss.")] Image cardBackground;
        [SerializeField, Tooltip("Dims the board behind a loss's card.")] GameObject dim;
        [SerializeField, Tooltip("Hidden while a win shows, so it fills the screen: the board and the HUD.")]
        GameObject[] hiddenByWin = Array.Empty<GameObject>();
        [SerializeField] GameObject fact;
        [SerializeField, Tooltip("\"Interesting Fact\", over the text.")] TMP_Text factHeading;
        [SerializeField] Image factImage;
        [SerializeField] TMP_Text factText;
        [SerializeField] TMP_Text factCredit;
        [SerializeField] Color earnedStarColour = new Color(1f, 0.8f, 0.25f);
        [SerializeField] Color missingStarColour = new Color(1f, 1f, 1f, 0.55f);

        [Header("Loss card")]
        [SerializeField, Tooltip("Room for the title, the stars at the win's size and the buttons, with space around them.")]
        Vector2 lossCardSize = new Vector2(980, 720);
        [SerializeField, Tooltip("Retry and Continue sit this far left and right of the card's centre.")]
        float lossButtonOffset = 110;

        [Header("Win layout")]
        [SerializeField, Min(0)] float photoMaxWidth = 940;
        [SerializeField, Min(0), Tooltip("The least space between the stars, the heading, the text, the photo and Continue; used on short screens.")]
        float gap = 30;
        [SerializeField, Min(0), Tooltip("On taller screens the spaces grow up to this.")]
        float maxGap = 80;
        [SerializeField, Min(0), Tooltip("Between the photo and its source.")]
        float creditGap = 12;
        [SerializeField, Min(0), Tooltip("A photo that would be shorter than this is left out rather than squeezed.")]
        float minPhotoHeight = 160;
        [SerializeField, Range(0, 1), Tooltip("On taller screens, Continue rises by this share of the height left once the spaces are at their most.")]
        float continueRise = 0.3f;
        [SerializeField, Range(0, 1), Tooltip("On taller screens, the title and stars move down by this share of the same height.")]
        float headerDrop = 0.2f;
        [SerializeField, Range(0, 1), Tooltip("Of the height left after that, this share goes above the fact and the rest below, so it sits a little above centre.")]
        float aboveShare = 0.4f;

        FactCard factCard;
        float continueY;
        float titleY;
        float starsY;
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
            if (!measured)
            {
                continueY = ((RectTransform)levelSelect.transform).anchoredPosition.y;
                titleY = title.rectTransform.anchoredPosition.y;
                starsY = stars.Length > 0 ? stars[0].rectTransform.anchoredPosition.y : 0;
                measured = true;
            }
            Hide();
        }

        public void Hide() => gameObject.SetActive(false);

        public void Show(SessionState outcome, int earnedStars)
        {
            bool won = outcome == SessionState.Won;
            title.SetText(won ? WonTitle : LostTitle);
            // A loss shows the three stars dimmed: what a win earns.
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(true);
                stars[i].color = won && i < earnedStars ? earnedStarColour : missingStarColour;
            }

            foreach (GameObject hidden in hiddenByWin)
                hidden.SetActive(!won);
            dim.SetActive(!won);
            cardBackground.enabled = !won;
            retry.gameObject.SetActive(!won);
            levelSelect.gameObject.SetActive(true);
            SetX(retry, -lossButtonOffset);
            SetPosition(levelSelect, won ? 0 : lossButtonOffset, continueY);
            LowerHeader(0);

            if (won)
            {
                card.anchorMin = Vector2.zero;
                card.anchorMax = Vector2.one;
                card.sizeDelta = Vector2.zero;
            }
            else
            {
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
                card.sizeDelta = lossCardSize;
            }
            card.anchoredPosition = Vector2.zero;

            if (won)
                ShowFact();
            else
                fact.SetActive(false);
            gameObject.SetActive(true);
        }

        // Moves the title and the stars down from where the scene puts them.
        void LowerHeader(float drop)
        {
            title.rectTransform.anchoredPosition = new Vector2(title.rectTransform.anchoredPosition.x, titleY - drop);
            foreach (Image star in stars)
                star.rectTransform.anchoredPosition = new Vector2(star.rectTransform.anchoredPosition.x, starsY - drop);
        }

        static void SetPosition(Button button, float x, float y) => ((RectTransform)button.transform).anchoredPosition = new Vector2(x, y);

        static void SetX(Button button, float x)
        {
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        }

        // Lays the fact out between the stars and Continue: the heading, the whole text (never cut), the photo in its own
        // shape and its source under the photo's left edge. The photo takes what is left at the least spacing; on a taller
        // screen the spaces grow up to their maximum; of the height still left, the title and stars come down by a share,
        // Continue rises by a share, and the rest sits mostly below the fact, so it rests a little above centre.
        void ShowFact()
        {
            bool hasText = !string.IsNullOrEmpty(factCard.Text);
            fact.SetActive(hasText || factCard.Image != null);

            RectTransform headingRect = factHeading.rectTransform, textRect = factText.rectTransform, creditRect = factCredit.rectTransform;
            factText.text = factCard.Text ?? "";
            float textHeight = hasText ? Mathf.Ceil(factText.GetPreferredValues(factText.text, textRect.rect.width, 0).y) : 0;
            textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, textHeight);

            // The space to fill, in the card's coordinates (0 at its top, negative downwards).
            RectTransform lastStar = stars[stars.Length - 1].rectTransform;
            var continueRect = (RectTransform)levelSelect.transform;
            float top = lastStar.anchoredPosition.y - lastStar.rect.height - gap;
            float bottom = -card.rect.height + continueY + continueRect.rect.height + gap;
            float space = top - bottom;

            float headingHeight = headingRect.rect.height;
            float creditBlock = string.IsNullOrEmpty(factCard.Credit) ? 0 : creditGap + creditRect.rect.height;
            Vector2 photo = factCard.Image != null
                ? Fit(factCard.Image.rect.size, photoMaxWidth, space - headingHeight - textHeight - creditBlock - 2 * gap)
                : Vector2.zero;
            bool showPhoto = photo.y >= minPhotoHeight;
            // The credit belongs to the photo: without the photo, it isn't shown.
            bool showCredit = showPhoto && creditBlock > 0;
            if (!showPhoto)
                photo = Vector2.zero;

            int gaps = showPhoto ? 2 : 1;
            float content = headingHeight + textHeight + photo.y + (showCredit ? creditBlock : 0);
            // Spare space is shared by the gaps and the margins above and below the block.
            float spare = space - content - gaps * gap;
            float spacing = gap + Mathf.Clamp(spare / (gaps + 2), 0, Mathf.Max(0, maxGap - gap));
            float left = Mathf.Max(0, space - content - gaps * spacing);
            float rise = left * continueRise;
            SetPosition(levelSelect, 0, continueY + rise);
            float drop = left * headerDrop;
            LowerHeader(drop);
            float y = top - drop - (left - drop - rise) * aboveShare;

            PlaceTop(headingRect, 0, y);
            y -= headingHeight + spacing;
            PlaceTop(textRect, 0, y);
            y -= textHeight + spacing;

            factImage.gameObject.SetActive(showPhoto);
            if (showPhoto)
            {
                factImage.sprite = factCard.Image;
                PlaceTop(factImage.rectTransform, 0, y);
                factImage.rectTransform.sizeDelta = photo;
            }

            factCredit.gameObject.SetActive(showCredit);
            factCredit.text = factCard.Credit ?? "";
            PlaceTop(creditRect, -photo.x / 2, y - photo.y - creditGap);
            creditRect.pivot = new Vector2(0f, 1f);
        }

        // Anchored to the card's top centre, hanging down from y.
        static void PlaceTop(RectTransform rect, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
        }

        // The largest size of the given shape that fits inside maxWidth x maxHeight.
        static Vector2 Fit(Vector2 shape, float maxWidth, float maxHeight)
        {
            if (shape.x <= 0 || shape.y <= 0 || maxHeight <= 0)
                return Vector2.zero;
            return shape * Mathf.Min(maxWidth / shape.x, maxHeight / shape.y);
        }
    }
}
