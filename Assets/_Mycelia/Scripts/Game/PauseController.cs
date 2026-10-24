using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mycelia.Game
{
    /// <summary>
    /// Pauses the level when the player looks away: the app going to the background, or (on device) losing focus to
    /// the notification shade, a call or split screen. Also handles the Android back button: pause, resume, and once
    /// the result is shown, leave the level (for level select, one screen up).
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] InputActionReference back;

        LevelPause pause;
        Action leave;

        public void Init(LevelPause pause, Action leave)
        {
            this.pause = pause;
            this.leave = leave;
        }

        /// <summary>What a press of back does; called from Update, and by tests.</summary>
        public void Back()
        {
            if (pause != null && !pause.Back())
                leave?.Invoke();
        }

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
            if (back.action.WasPressedThisFrame())
                Back();
        }
    }
}
