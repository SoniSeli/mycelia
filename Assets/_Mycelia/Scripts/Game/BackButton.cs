using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mycelia.Game
{
    /// <summary>
    /// The Android back button on a menu screen (ADR-0013): back steps out one screen, and on the first screen it
    /// sends the app to the background. The scene's entry decides which, through <see cref="Init"/>.
    /// </summary>
    public sealed class BackButton : MonoBehaviour
    {
        [SerializeField] InputActionReference back;

        Action onBack;

        public void Init(Action onBack) => this.onBack = onBack;

        /// <summary>What a press of back does; called from Update, and by tests.</summary>
        public void Press() => onBack?.Invoke();

        void OnEnable() => back.action.Enable();

        void OnDisable() => back.action.Disable();

        void Update()
        {
            if (back.action.WasPressedThisFrame())
                Press();
        }
    }
}
