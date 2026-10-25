using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>Draws a <see cref="Toggle"/> as a switch from the UI kit: the knob slides to the on side and the track takes the on colour.</summary>
    public sealed class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] Toggle toggle;
        [SerializeField] Image track;
        [SerializeField] RectTransform knob;
        [SerializeField] Color onColour = new Color(0.24f, 0.58f, 0.44f);
        [SerializeField] Color offColour = new Color(0.3f, 0.34f, 0.4f);
        [SerializeField, Tooltip("How far the knob sits from the track's centre, either side.")] float travel = 44;

        void OnEnable()
        {
            toggle.onValueChanged.AddListener(Show);
            Show(toggle.isOn);
        }

        void OnDisable() => toggle.onValueChanged.RemoveListener(Show);

        void Show(bool on)
        {
            knob.anchoredPosition = new Vector2(on ? travel : -travel, 0);
            track.color = on ? onColour : offColour;
        }
    }
}
