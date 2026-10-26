using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// Every button in a scene taps when pressed (design §8.3 slice 10), hidden ones and the settings modal's included.
    /// Scene entries call it last in <c>Construct</c>, after their views have made any buttons of their own.
    /// </summary>
    public static class ButtonTaps
    {
        public static void Attach(Scene scene, ISoundEffects sounds)
        {
            if (sounds == null)
                throw new ArgumentNullException(nameof(sounds));

            UnityAction tap = () => sounds.Play(SoundEffect.Tap);
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Button button in root.GetComponentsInChildren<Button>(includeInactive: true))
                    button.onClick.AddListener(tap);
        }
    }
}
