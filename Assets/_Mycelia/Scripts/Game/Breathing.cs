using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Grows its object a little and back, slowly and without end, eased in and out like a breath: the main menu's mushroom
    /// (design §8.3). Only the scale changes, so the object belongs on its own canvas (ADR-0012 rule 1).
    /// </summary>
    public sealed class Breathing : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Seconds per breath, in and out.")]
        float period = 3f;
        [SerializeField, Range(0f, 0.5f), Tooltip("How much larger at the top of a breath, as a fraction of the resting size.")]
        float amount = 0.05f;

        float startTime;

        public float Period => period;
        public float Amount => amount;

        /// <summary>The scale <paramref name="seconds"/> into breathing: 1 at rest, 1 + amount halfway through each breath.</summary>
        public static float ScaleAt(float seconds, float period, float amount) =>
            1f + amount * 0.5f * (1f - Mathf.Cos(seconds / period * 2f * Mathf.PI));

        // Each time the screen shows, the first breath starts from resting size.
        void OnEnable() => startTime = Time.unscaledTime;

        void OnDisable() => transform.localScale = Vector3.one;

        void Update()
        {
            float scale = ScaleAt(Time.unscaledTime - startTime, period, amount);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
