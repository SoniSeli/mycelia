using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>What one level select button shows (design §3.6).</summary>
    public readonly struct LevelButtonState
    {
        /// <summary>1-based position in the catalog.</summary>
        public readonly int Number;
        public readonly bool Unlocked;

        /// <summary>0 for a level never won.</summary>
        public readonly int BestStars;

        public LevelButtonState(int number, bool unlocked, int bestStars)
        {
            Number = number;
            Unlocked = unlocked;
            BestStars = bestStars;
        }
    }

    /// <summary>
    /// One level on the level select grid, on Soni's chunky node: bright with its number and best stars, lit as earned,
    /// or dimmed with a lock and no reaction to taps until the level before it is won.
    /// </summary>
    public sealed class LevelButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField, Tooltip("The node shape, coloured open or locked.")] Image node;
        [SerializeField] TMP_Text number;
        [SerializeField] Image lockIcon;
        [SerializeField] Image[] stars = Array.Empty<Image>();
        [SerializeField] Color earnedStarColour = new Color(1f, 0.8f, 0.25f);
        [SerializeField] Color missingStarColour = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] Color openColour = new Color(0.765f, 0.937f, 0.847f);
        [SerializeField] Color lockedColour = new Color(0.765f, 0.937f, 0.847f, 0.35f);

        public Color EarnedStarColour => earnedStarColour;
        public Color MissingStarColour => missingStarColour;
        public Color OpenColour => openColour;
        public Color LockedColour => lockedColour;
        public bool IsUnlocked => button.interactable;

        public void Show(LevelButtonState state, Action onClick)
        {
            number.SetText("{0}", state.Number);
            number.gameObject.SetActive(state.Unlocked);
            node.color = state.Unlocked ? openColour : lockedColour;
            lockIcon.gameObject.SetActive(!state.Unlocked);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(state.Unlocked);
                stars[i].color = i < state.BestStars ? earnedStarColour : missingStarColour;
            }

            button.interactable = state.Unlocked;
            button.onClick.RemoveAllListeners();
            if (state.Unlocked)
                button.onClick.AddListener(() => onClick());
        }
    }
}
