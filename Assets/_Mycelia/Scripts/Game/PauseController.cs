using UnityEngine;
using UnityEngine.InputSystem;

namespace Mycelia.Game
{
    /// <summary>
    /// Pauses the level when the player looks away: the app going to the background, or (on device) losing focus to
    /// the notification shade, a call or split screen. Also handles the Android back button: pause, resume, and once
    /// the result is shown, send the app to the background like any first screen.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] InputActionReference back;

        LevelPause pause;

        public void Init(LevelPause pause) => this.pause = pause;

        void OnEnable() => back.action.Enable();

        void OnDisable() => back.action.Disable();

        void OnApplicationPause(bool paused)
        {
            if (paused)
                pause?.Pause();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            // In the Editor, focus moves to other windows all the time; only the player pauses on it.
            if (!hasFocus && !Application.isEditor)
                pause?.Pause();
        }

        void Update()
        {
            if (pause != null && back.action.WasPressedThisFrame() && !pause.Back())
                AndroidApp.MoveToBackground();
        }
    }
}
