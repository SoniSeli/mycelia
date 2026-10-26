using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// The level select screen: one <see cref="LevelButton"/> per level, in play order, on a fixed three-column grid
    /// that fits every level without scrolling. The buttons are made once when the screen opens; a menu canvas may
    /// lay them out with a grid layout group (ADR-0012 rule 3). Help and settings sit in a footer below.
    /// </summary>
    public sealed class LevelSelectView : MonoBehaviour
    {
        [SerializeField, Tooltip("Copied once per level.")]
        LevelButton buttonPrefab;
        [SerializeField, Tooltip("Holds the buttons; its GridLayoutGroup places them.")]
        RectTransform grid;
        [SerializeField] Button settings;
        [SerializeField, Tooltip("Replays the intro (design §8.3 slice 8).")]
        Button help;

        readonly List<LevelButton> buttons = new List<LevelButton>();

        public IReadOnlyList<LevelButton> Buttons => buttons;

        /// <param name="onChoose">Called with the chosen level's position in the catalog (0-based).</param>
        public void Init(IReadOnlyList<LevelButtonState> levels, Action<int> onChoose, Action onSettings, Action onHelp)
        {
            settings.onClick.RemoveAllListeners();
            settings.onClick.AddListener(() => onSettings());
            help.onClick.RemoveAllListeners();
            help.onClick.AddListener(() => onHelp());
            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                LevelButton button = Instantiate(buttonPrefab, grid);
                button.name = $"Level {levels[i].Number}";
                button.gameObject.SetActive(true);
                button.Show(levels[i], () => onChoose(index));
                buttons.Add(button);
            }
        }
    }
}
