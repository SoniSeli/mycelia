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
        public readonly BoardLayoutSettings Layout;

        public TestHud()
        {
            Root = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas));
            Layout = ScriptableObject.CreateInstance<BoardLayoutSettings>();

            Band = Child("Band", Root.transform);
            Moves = Child("Moves", Band).gameObject.AddComponent<TextMeshProUGUI>();
            Slots = Child("Objectives", Band);

            RectTransform slotTemplate = Child("Slot Template", Root.transform);
            slotTemplate.sizeDelta = new Vector2(200, 120);
            var slot = slotTemplate.gameObject.AddComponent<ObjectiveSlot>();
            Set(slot, "icon", Child("Icon", slotTemplate).gameObject.AddComponent<Image>());
            Set(slot, "count", Child("Count", slotTemplate).gameObject.AddComponent<TextMeshProUGUI>());
            slotTemplate.gameObject.SetActive(false);

            RectTransform panelRoot = Child("Result Panel", Root.transform);
            Panel = panelRoot.gameObject.AddComponent<ResultPanel>();
            Set(Panel, "title", Child("Title", panelRoot).gameObject.AddComponent<TextMeshProUGUI>());
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++) stars[i] = Child($"Star {i + 1}", panelRoot).gameObject.AddComponent<Image>();
            SetArray(Panel, "stars", stars);
            Set(Panel, "retry", Child("Retry", panelRoot).gameObject.AddComponent<Button>());
            Set(Panel, "levelSelect", Child("Level Select", panelRoot).gameObject.AddComponent<Button>());
            RectTransform card = Child("Card", panelRoot);
            card.sizeDelta = new Vector2(920, 100);
            Set(Panel, "card", card);
            RectTransform fact = Child("Fact", panelRoot);
            Set(Panel, "fact", fact.gameObject);
            Set(Panel, "factImage", Child("Fact Image", fact).gameObject.AddComponent<Image>());
            Set(Panel, "factText", Child("Fact Text", fact).gameObject.AddComponent<TextMeshProUGUI>());
            Set(Panel, "factCredit", Child("Fact Credit", fact).gameObject.AddComponent<TextMeshProUGUI>());

            Hud = Root.AddComponent<HudView>();
            Set(Hud, "band", Band);
            Set(Hud, "layout", Layout);
            Set(Hud, "movesText", Moves);
            Set(Hud, "slotPrefab", slot);
            Set(Hud, "slotsRoot", Slots);
            Set(Hud, "resultPanel", Panel);
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Root);
            Object.DestroyImmediate(Layout);
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
