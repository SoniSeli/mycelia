using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>
    /// Covers the level while it is paused (design §6) and waits for the player: Resume carries on, Restart starts the
    /// level again with a fresh seed, and Quit leaves it for level select (a level in progress is not saved, ADR-0006).
    /// </summary>
    public sealed class PauseOverlay : MonoBehaviour, IPausable
    {
        [SerializeField] Button resume;
        [SerializeField] Button restart;
        [SerializeField, FormerlySerializedAs("levelSelect")] Button quit;

        public void Init(Action onResume, Action onRestart, Action onQuit)
        {
            resume.onClick.RemoveAllListeners();
            resume.onClick.AddListener(() => onResume());
            restart.onClick.RemoveAllListeners();
            restart.onClick.AddListener(() => onRestart());
            quit.onClick.RemoveAllListeners();
            quit.onClick.AddListener(() => onQuit());
            SetPaused(false);
        }

        public void SetPaused(bool paused) => gameObject.SetActive(paused);
    }
}
