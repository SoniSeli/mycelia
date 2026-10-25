using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>The title screen: the game's name, Play, and the settings button.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] Button play;
        [SerializeField] Button settings;

        public void Init(Action onPlay, Action onSettings)
        {
            play.onClick.RemoveAllListeners();
            play.onClick.AddListener(() => onPlay());
            settings.onClick.RemoveAllListeners();
            settings.onClick.AddListener(() => onSettings());
        }
    }
}
