using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// Covers the level while it is paused (design §6) and waits for the player: Resume carries on, Restart starts the
    /// level again with a fresh seed, and Level select leaves it (a level in progress is not saved, ADR-0006).
    /// </summary>
    public sealed class PauseOverlay : MonoBehaviour, IPausable
    {
        [SerializeField] Button resume;
        [SerializeField] Button restart;
        [SerializeField] Button levelSelect;

        public void Init(Action onResume, Action onRestart, Action onLevelSelect)
        {
            resume.onClick.RemoveAllListeners();
            resume.onClick.AddListener(() => onResume());
            restart.onClick.RemoveAllListeners();
            restart.onClick.AddListener(() => onRestart());
            levelSelect.onClick.RemoveAllListeners();
            levelSelect.onClick.AddListener(() => onLevelSelect());
            SetPaused(false);
        }

        public void SetPaused(bool paused) => gameObject.SetActive(paused);
    }
}
