using System;
using Mycelia.Domain.Sessions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// Shows how the level ended (design §3.4–3.5): "Level complete" with 1–3 stars, or "Out of moves", and a Play
    /// again button. Level select and the fact card arrive in Milestone 3.
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        public const string WonTitle = "Level complete";
        public const string LostTitle = "Out of moves";

        [SerializeField] TMP_Text title;
        [SerializeField] Image[] stars = Array.Empty<Image>();
        [SerializeField] Button playAgain;
        [SerializeField] Color earnedStarColour = new Color(1f, 0.8f, 0.25f);
        [SerializeField] Color missingStarColour = new Color(1f, 1f, 1f, 0.2f);

        public Color EarnedStarColour => earnedStarColour;
        public Color MissingStarColour => missingStarColour;

        public void Init(Action onPlayAgain)
        {
            playAgain.onClick.RemoveAllListeners();
            playAgain.onClick.AddListener(() => onPlayAgain());
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
            gameObject.SetActive(true);
        }
    }
}
