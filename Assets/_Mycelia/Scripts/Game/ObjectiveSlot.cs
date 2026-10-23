using Mycelia.Content;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game
{
    /// <summary>One goal in the HUD: the spore's silhouette in its colour, and collected/target.</summary>
    public sealed class ObjectiveSlot : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text count;

        Tween pulse;

        public void Show(SporeType type, int target)
        {
            icon.sprite = type.Sprite;
            icon.color = type.Color;
            icon.preserveAspect = true;
            SetProgress(0, target);
        }

        public void SetProgress(int current, int target)
        {
            // Numeric SetText doesn't allocate (ADR-0012 rule 5).
            count.SetText("{0}/{1}", current, target);
        }

        /// <summary>A short bounce so the player notices the count change.</summary>
        public void Pulse()
        {
            if (pulse.isAlive)
                pulse.Complete();
            pulse = Tween.PunchScale(transform, new Vector3(0.2f, 0.2f, 0f), duration: 0.25f, frequency: 6);
        }

        void OnDestroy()
        {
            if (pulse.isAlive)
                pulse.Stop();
        }
    }
}
