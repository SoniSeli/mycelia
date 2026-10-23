using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// Covers the level while it is paused (design §6) and waits for the player: Resume carries on, Restart starts the
    /// level again with a fresh seed. Level select arrives with the app shell (Milestone 4).
    /// </summary>
    public sealed class PauseOverlay : MonoBehaviour, IPausable
    {
        [SerializeField] Button resume;
        [SerializeField] Button restart;

        public void Init(Action onResume, Action onRestart)
        {
            resume.onClick.RemoveAllListeners();
            resume.onClick.AddListener(() => onResume());
            restart.onClick.RemoveAllListeners();
            restart.onClick.AddListener(() => onRestart());
            SetPaused(false);
        }

        public void SetPaused(bool paused) => gameObject.SetActive(paused);
    }
}
