using System.IO;
using System.Xml;

namespace Mycelia.App.Editor
{
    /// <summary>
    /// Adds the VIBRATE permission to the Android build's manifest (ADR-0016). Unity adds it on its own only when code
    /// calls <c>Handheld.Vibrate</c>, which the game doesn't: <see cref="AndroidHaptics"/> calls the vibrator directly.
    /// VIBRATE is a normal permission, granted at install without asking the player.
    /// </summary>
    public static class AndroidVibratePermission
    {
        public const string Name = "android.permission.VIBRATE";
        const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

        /// <summary>Adds the permission to <paramref name="manifest"/> unless it is already there.</summary>
        /// <returns>True if the manifest changed.</returns>
        public static bool AddTo(XmlDocument manifest)
        {
            XmlElement root = manifest.DocumentElement;
            foreach (XmlNode node in root.ChildNodes)
                if (node is XmlElement element && element.Name == "uses-permission" && element.GetAttribute("name", AndroidNamespace) == Name)
                    return false;

            XmlElement permission = manifest.CreateElement("uses-permission");
            permission.SetAttribute("name", AndroidNamespace, Name);
            root.PrependChild(permission);
            return true;
        }

        /// <summary>Adds the permission to the manifest file under <paramref name="libraryPath"/> (Unity's unityLibrary module).</summary>
        public static void AddToProject(string libraryPath)
        {
            string path = Path.Combine(libraryPath, "src", "main", "AndroidManifest.xml");
            var manifest = new XmlDocument { PreserveWhitespace = true };
            manifest.Load(path);
            if (AddTo(manifest))
                manifest.Save(path);
        }
    }

#if UNITY_ANDROID
    // Only where the Android build support is installed: the CI test runner has none.
    sealed class AndroidVibratePermissionStep : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path) => AndroidVibratePermission.AddToProject(path);
    }
#endif
}
