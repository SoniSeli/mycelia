using PrimeTween;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>Durations and easings of move playback, tuned by feel without code changes (ADR-0009).</summary>
    [CreateAssetMenu(menuName = "Mycelia/Playback Settings", fileName = "PlaybackSettings")]
    public sealed class PlaybackSettings : ScriptableObject
    {
        [Header("Pop")]
        [SerializeField, Min(0.01f), Tooltip("Seconds for a cleared spore to shrink and fade.")]
        float clearDuration = 0.15f;
        [SerializeField] Ease clearEase = Ease.InBack;

        [Header("Fall")]
        [SerializeField, Min(0.01f), Tooltip("Seconds to fall one cell. Longer falls take this × √cells, like gravity.")]
        float fallDurationPerCell = 0.14f;
        [SerializeField] Ease fallEase = Ease.OutBack;

        [Header("Refill")]
        [SerializeField, Min(0.01f), Tooltip("Seconds for a new spore to fade in as it falls in from above the board.")]
        float spawnFadeDuration = 0.12f;

        [Header("Reshuffle")]
        [SerializeField, Min(0.01f)] float relocateDuration = 0.35f;
        [SerializeField] Ease relocateEase = Ease.InOutQuad;

        [Header("Capacity")]
        [SerializeField, Min(64), Tooltip("Tweens allocated up front, so playback never allocates (ADR-0009). Covers the largest move with headroom.")]
        int tweenCapacity = 512;

        public float ClearDuration => clearDuration;
        public Ease ClearEase => clearEase;
        public Ease FallEase => fallEase;
        public float SpawnFadeDuration => spawnFadeDuration;
        public float RelocateDuration => relocateDuration;
        public Ease RelocateEase => relocateEase;
        public int TweenCapacity => tweenCapacity;

        /// <summary>Seconds to fall <paramref name="cells"/> cells; at least one cell's time.</summary>
        public float FallDuration(int cells) => fallDurationPerCell * Mathf.Sqrt(Mathf.Max(1, cells));
    }
}
