using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>Calls into the Android activity. Does nothing on other platforms or in the Editor.</summary>
    public static class AndroidApp
    {
        /// <summary>
        /// Sends the app to the background without finishing it, as Android does for back on an app's first screen.
        /// Coming back returns to the same screen.
        /// </summary>
        public static void MoveToBackground()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call<bool>("moveTaskToBack", true);
#endif
        }
    }
}
