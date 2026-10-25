using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    /// <summary>Builds a HUD and result panel from scene objects, wired like the Game scene's, under one root to destroy.</summary>
    sealed class TestHud
    {
        public readonly GameObject Root;
        public readonly HudView Hud;
        public readonly ResultPanel Panel;
        public readonly TMP_Text Moves;
        public readonly RectTransform Band;
        public readonly RectTransform Slots;
        public readonly Button PauseButton;
        public readonly Button SettingsButton;
        public readonly TMP_Text Title;
        public readonly BoardLayoutSettings Layout;
        public readonly GameObject Board;

        public TestHud()
        {
            Root = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas));
            Layout = ScriptableObject.CreateInstance<BoardLayoutSettings>();

            Band = Child("Band", Root.transform);
            Moves = Child("Moves", Band).gameObject.AddComponent<TextMeshProUGUI>();
            Slots = Child("Objectives", Band);
            PauseButton = Child("Pause Button", Root.transform).gameObject.AddComponent<Button>();
            SettingsButton = Child("Settings Button", Root.transform).gameObject.AddComponent<Button>();
            Title = Child("Title", Band).gameObject.AddComponent<TextMeshProUGUI>();

            RectTransform slotTemplate = Child("Slot Template", Root.transform);
            slotTemplate.sizeDelta = new Vector2(200, 120);
            var slot = slotTemplate.gameObject.AddComponent<ObjectiveSlot>();
            Set(slot, "icon", Child("Icon", slotTemplate).gameObject.AddComponent<Image>());
            Set(slot, "count", Child("Count", slotTemplate).gameObject.AddComponent<TextMeshProUGUI>());
            slotTemplate.gameObject.SetActive(false);

            // The result panel fills the safe area (a phone's, at the reference width); the card inside it holds everything.
            RectTransform panelRoot = Child("Result Panel", Root.transform);
            panelRoot.sizeDelta = new Vector2(1080, 2250);
            Panel = panelRoot.gameObject.AddComponent<ResultPanel>();
            RectTransform dim = Child("Dim", panelRoot);
            dim.gameObject.AddComponent<Image>();
            Set(Panel, "dim", dim.gameObject);
            RectTransform card = Child("Card", panelRoot);
            Set(Panel, "card", card);
            Set(Panel, "cardBackground", card.gameObject.AddComponent<Image>());
            RectTransform panelTitle = TopChild("Title", card, -60);
            panelTitle.sizeDelta = new Vector2(1000, 130);
            Set(Panel, "title", panelTitle.gameObject.AddComponent<TextMeshProUGUI>());
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                RectTransform star = TopChild($"Star {i + 1}", card, -200);
                star.sizeDelta = new Vector2(130, 130);
                stars[i] = star.gameObject.AddComponent<Image>();
            }
            SetArray(Panel, "stars", stars);
            Set(Panel, "retry", BottomButton("Retry", card));
            Set(Panel, "levelSelect", BottomButton("Level Select", card));
            RectTransform fact = Child("Fact", card);
            Set(Panel, "fact", fact.gameObject);
            RectTransform heading = TopChild("Interesting Fact", fact, 0);
            heading.sizeDelta = new Vector2(940, 90);
            Set(Panel, "factHeading", heading.gameObject.AddComponent<TextMeshProUGUI>());
            Set(Panel, "factImage", TopChild("Fact Image", fact, 0).gameObject.AddComponent<Image>());
            RectTransform factText = TopChild("Fact Text", fact, -500);
            factText.sizeDelta = new Vector2(940, 100);
            Set(Panel, "factText", factText.gameObject.AddComponent<TextMeshProUGUI>());
            RectTransform credit = TopChild("Fact Credit", fact, 0);
            credit.sizeDelta = new Vector2(940, 40);
            Set(Panel, "factCredit", credit.gameObject.AddComponent<TextMeshProUGUI>());
            fact.anchorMin = Vector2.zero;
            fact.anchorMax = Vector2.one;
            fact.sizeDelta = Vector2.zero;

            // Hidden by a win: stand-ins for the board and the HUD.
            Board = new GameObject("Board");
            Board.transform.SetParent(Root.transform, false);
            SetArray(Panel, "hiddenByWin", new Object[] { Board, Band.gameObject });

            Hud = Root.AddComponent<HudView>();
            Set(Hud, "band", Band);
            Set(Hud, "layout", Layout);
            Set(Hud, "movesText", Moves);
            Set(Hud, "slotPrefab", slot);
            Set(Hud, "slotsRoot", Slots);
            Set(Hud, "pauseButton", PauseButton);
            Set(Hud, "settingsButton", SettingsButton);
            Set(Hud, "title", Title);
            Set(Hud, "resultPanel", Panel);
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Root);
            Object.DestroyImmediate(Layout);
        }

        // A round button anchored to the card's bottom centre, like the scene's.
        static Button BottomButton(string name, Transform card)
        {
            RectTransform rect = Child(name, card);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 40);
            rect.sizeDelta = new Vector2(150, 150);
            rect.gameObject.AddComponent<Image>();
            return rect.gameObject.AddComponent<Button>();
        }

        // Anchored to the top centre of its parent, hanging down from y.
        static RectTransform TopChild(string name, Transform parent, float y)
        {
            RectTransform rect = Child(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, y);
            return rect;
        }

        static RectTransform Child(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        public static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string property, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
