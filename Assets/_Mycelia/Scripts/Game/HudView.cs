using System;
using Mycelia.Content;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Sessions;
using TMPro;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// The level's HUD (ADR-0012): moves left and one slot per goal, in the top band the board leaves free (ADR-0011).
    /// Playback reports changes as they happen on screen; updates allocate nothing. The end of the level opens the
    /// result panel.
    /// </summary>
    public sealed class HudView : MonoBehaviour, IPlaybackListener
    {
        [SerializeField, Tooltip("Fitted to the top band of the safe area that BoardLayoutSettings keeps free.")]
        RectTransform band;
        [SerializeField] BoardLayoutSettings layout;
        [SerializeField] TMP_Text movesText;
        [SerializeField] ObjectiveSlot slotPrefab;
        [SerializeField, Tooltip("Goals are laid out side by side here, centred; no layout group (ADR-0012 rule 3).")]
        RectTransform slotsRoot;
        [SerializeField, Min(0)] float slotSpacing = 24;
        [SerializeField] ResultPanel resultPanel;

        ObjectiveSlot[] slots = Array.Empty<ObjectiveSlot>();

        public void Init(LevelConfig config, SporeCatalog catalog)
        {
            BoardInsets insets = layout.Insets;
            band.anchorMin = new Vector2(insets.Side, 1 - insets.Top);
            band.anchorMax = new Vector2(1 - insets.Side, 1);
            band.offsetMin = band.offsetMax = Vector2.zero;

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
            float width = ((RectTransform)slotPrefab.transform).rect.width + slotSpacing;
            for (int i = 0; i < count; i++)
            {
                ObjectiveDefinition goal = config.Objectives[i];
                ObjectiveSlot slot = Instantiate(slotPrefab, slotsRoot);
                slot.name = $"Goal {i + 1}";
                slot.gameObject.SetActive(true);
                slot.Show(catalog.TypeOf(goal.Type), goal.Target);

                var rect = (RectTransform)slot.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2((i - (count - 1) / 2f) * width, 0);
                slots[i] = slot;
            }
        }
    }
}
