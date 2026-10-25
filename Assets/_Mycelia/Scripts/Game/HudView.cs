using System;
using Mycelia.Content;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Sessions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// The level's HUD (ADR-0012): one card with moves left and a slot per goal in the top band the board leaves free,
    /// and a pause button in the band below it (ADR-0011).
    /// Playback reports changes as they happen on screen; updates allocate nothing. The end of the level opens the
    /// result panel.
    /// </summary>
    public sealed class HudView : MonoBehaviour, IPlaybackListener
    {
        [SerializeField, Tooltip("Fitted to the top band of the safe area that BoardLayoutSettings keeps free.")]
        RectTransform band;
        [SerializeField] BoardLayoutSettings layout;
        [SerializeField, Tooltip("\"Level N\" on the tab above the card.")] TMP_Text title;
        [SerializeField] Button pauseButton;
        [SerializeField] TMP_Text movesText;
        [SerializeField] ObjectiveSlot slotPrefab;
        [SerializeField, Tooltip("Goals share this area evenly, side by side; no layout group (ADR-0012 rule 3).")]
        RectTransform slotsRoot;
        [SerializeField] ResultPanel resultPanel;

        ObjectiveSlot[] slots = Array.Empty<ObjectiveSlot>();

        public void Init(LevelConfig config, int levelNumber, SporeCatalog catalog, Action onPause)
        {
            BoardInsets insets = layout.Insets;
            band.anchorMin = new Vector2(insets.Side, 1 - insets.Top);
            band.anchorMax = new Vector2(1 - insets.Side, 1);
            band.offsetMin = band.offsetMax = Vector2.zero;

            // Centred in the free band below the board.
            var pause = (RectTransform)pauseButton.transform;
            pause.anchorMin = pause.anchorMax = new Vector2(0.5f, insets.Bottom / 2);
            pause.anchoredPosition = Vector2.zero;
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(() => onPause());
            // A level outside the catalog (the Editor's debug level) has no number.
            if (levelNumber > 0)
                title.SetText("Level {0}", levelNumber);
            else
                title.SetText("Level");
            MovesChanged(config.Moves);
            CreateSlots(config, catalog);
            resultPanel.Hide();
        }

        public void MovesChanged(int remaining) => movesText.SetText("{0}", remaining);

        public void ObjectiveProgressed(int index, int current, int target)
        {
            slots[index].SetProgress(current, target);
            slots[index].Pulse();
        }

        public void SessionEnded(SessionState outcome, int stars) => resultPanel.Show(outcome, stars);

        void CreateSlots(LevelConfig config, SporeCatalog catalog)
        {
            int count = config.Objectives.Count;
            slots = new ObjectiveSlot[count];
            for (int i = 0; i < count; i++)
            {
                ObjectiveDefinition goal = config.Objectives[i];
                ObjectiveSlot slot = Instantiate(slotPrefab, slotsRoot);
                slot.name = $"Goal {i + 1}";
                slot.gameObject.SetActive(true);
                slot.Show(catalog.TypeOf(goal.Type), goal.Target);

                // An equal share of the goal area each, side by side: up to four fit on the narrowest screen.
                var rect = (RectTransform)slot.transform;
                rect.anchorMin = new Vector2(i / (float)count, 0);
                rect.anchorMax = new Vector2((i + 1) / (float)count, 1);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                slots[i] = slot;
            }
        }
    }
}
