using Mycelia.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.App.Tests
{
    /// <summary>A settings modal built from scene objects, wired like the prefab's, for the scene entry tests.</summary>
    sealed class TestSettingsModal
    {
        public readonly SettingsModal Modal;
        public readonly Toggle Effects;
        public readonly Toggle Music;
        public readonly Toggle Haptics;
        public readonly Button Close;

        public TestSettingsModal(Transform parent)
        {
            var root = new GameObject("Settings", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Modal = root.AddComponent<SettingsModal>();
            Effects = Child<Toggle>("Effects", root.transform);
            Music = Child<Toggle>("Music", root.transform);
            Haptics = Child<Toggle>("Haptics", root.transform);
            Close = Child<Button>("Close", root.transform);
            Set(Modal, "effects", Effects);
            Set(Modal, "music", Music);
            Set(Modal, "haptics", Haptics);
            Set(Modal, "close", Close);
            Set(Modal, "backdrop", Child<Button>("Backdrop", root.transform));
        }

        static T Child<T>(string name, Transform parent) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }

        static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
