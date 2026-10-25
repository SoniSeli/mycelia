using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    /// <summary>A settings modal built from scene objects, wired like the prefab's.</summary>
    sealed class TestSettings
    {
        public readonly SettingsModal Modal;
        public readonly Toggle Effects;
        public readonly Toggle Music;
        public readonly Toggle Haptics;
        public readonly Button Close;
        public readonly Button Backdrop;

        public TestSettings(Transform parent = null)
        {
            var root = new GameObject("Settings", typeof(RectTransform));
            if (parent != null) root.transform.SetParent(parent, false);
            Modal = root.AddComponent<SettingsModal>();
            Effects = Child<Toggle>("Effects", root.transform);
            Music = Child<Toggle>("Music", root.transform);
            Haptics = Child<Toggle>("Haptics", root.transform);
            Close = Child<Button>("Close", root.transform);
            Backdrop = Child<Button>("Backdrop", root.transform);
            TestHud.Set(Modal, "effects", Effects);
            TestHud.Set(Modal, "music", Music);
            TestHud.Set(Modal, "haptics", Haptics);
            TestHud.Set(Modal, "close", Close);
            TestHud.Set(Modal, "backdrop", Backdrop);
        }

        public void Destroy() => Object.DestroyImmediate(Modal.gameObject);

        static T Child<T>(string name, Transform parent) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }
    }
}
