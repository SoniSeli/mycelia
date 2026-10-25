using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// The settings modal shared by the main menu, level select and the level (design §8.3 slice 5): switches for sound
    /// effects, music and haptics, and a note that colour-blind support is built in. Every flip is reported at once,
    /// so it can be saved on change. The close button or a tap on the dimmed screen around the panel closes it.
    /// </summary>
    public sealed class SettingsModal : MonoBehaviour
    {
        [SerializeField] Toggle effects;
        [SerializeField] Toggle music;
        [SerializeField] Toggle haptics;
        [SerializeField] Button close;
        [SerializeField, Tooltip("The dimmed screen behind the panel: it takes every tap, and a tap closes the modal.")]
        Button backdrop;

        Action<SettingsChoice> onChanged;
        Action onClosed;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(Action<SettingsChoice> onChanged, Action onClosed)
        {
            this.onChanged = onChanged;
            this.onClosed = onClosed;
            foreach (Toggle toggle in new[] { effects, music, haptics })
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(_ => this.onChanged?.Invoke(Choice));
            }
            close.onClick.RemoveAllListeners();
            close.onClick.AddListener(Close);
            backdrop.onClick.RemoveAllListeners();
            backdrop.onClick.AddListener(Close);
            Hide();
        }

        /// <summary>Shows <paramref name="current"/> without reporting it as a change.</summary>
        public void Open(SettingsChoice current)
        {
            effects.SetIsOnWithoutNotify(current.Effects);
            music.SetIsOnWithoutNotify(current.Music);
            haptics.SetIsOnWithoutNotify(current.Haptics);
            gameObject.SetActive(true);
        }

        /// <summary>The player closes it: hides it and reports that.</summary>
        public void Close()
        {
            if (!IsOpen)
                return;
            Hide();
            onClosed?.Invoke();
        }

        /// <summary>Hides it without reporting, when something else ends what opened it (the level resuming).</summary>
        public void Hide() => gameObject.SetActive(false);

        SettingsChoice Choice => new SettingsChoice(effects.isOn, music.isOn, haptics.isOn);
    }
}
