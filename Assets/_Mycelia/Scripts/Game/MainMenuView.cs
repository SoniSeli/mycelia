using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>The title screen: the game's name and Play. Settings join it in Milestone 4.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] Button play;

        public void Init(Action onPlay)
        {
            play.onClick.RemoveAllListeners();
            play.onClick.AddListener(() => onPlay());
        }
    }
}
