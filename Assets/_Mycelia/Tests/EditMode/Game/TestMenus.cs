using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    /// <summary>Builds level buttons, the level select view and the main menu view from scene objects, wired like the menu scenes.</summary>
    static class TestMenus
    {
        public static LevelButton Button(Transform parent = null)
        {
            RectTransform root = Child("Level Button", parent);
            root.gameObject.AddComponent<Image>();
            var button = root.gameObject.AddComponent<Button>();
            var level = root.gameObject.AddComponent<LevelButton>();
            Set(level, "button", button);
            Set(level, "number", Child("Number", root).gameObject.AddComponent<TextMeshProUGUI>());
            Set(level, "lockIcon", Child("Lock", root).gameObject.AddComponent<Image>());
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++) stars[i] = Child($"Star {i + 1}", root).gameObject.AddComponent<Image>();
            SetArray(level, "stars", stars);
            return level;
        }

        public static LevelSelectView LevelSelect(out LevelButton template)
        {
            RectTransform root = Child("Level Select", null);
            var view = root.gameObject.AddComponent<LevelSelectView>();
            RectTransform grid = Child("Grid", root);
            template = Button(root);
            template.gameObject.SetActive(false);
            Set(view, "buttonPrefab", template);
            Set(view, "grid", grid);
            Set(view, "settings", Child("Settings", root).gameObject.AddComponent<Button>());
            return view;
        }

        public static MainMenuView MainMenu()
        {
            RectTransform root = Child("Main Menu", null);
            var view = root.gameObject.AddComponent<MainMenuView>();
            RectTransform play = Child("Play", root);
            play.gameObject.AddComponent<Image>();
            Set(view, "play", play.gameObject.AddComponent<Button>());
            Set(view, "settings", Child("Settings", root).gameObject.AddComponent<Button>());
            return view;
        }

        static RectTransform Child(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            if (parent != null) child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        static void Set(Object target, string property, Object value)
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
