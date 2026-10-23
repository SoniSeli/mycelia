using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>Touch tuning for chains, adjusted on device without code changes (ADR-0008).</summary>
    [CreateAssetMenu(menuName = "Mycelia/Chain Input Settings", fileName = "ChainInputSettings")]
    public sealed class ChainInputSettings : ScriptableObject
    {
        [SerializeField, Range(0.25f, 0.5f), Tooltip("A spore is touched only within this distance of its centre, in cells. Keeps diagonal drags off the side neighbours.")]
        float hitRadius = 0.4f;

        [SerializeField, Range(0.1f, 1f), Tooltip("A fast swipe is checked at least this often along its path, in cells, so it can't skip a spore.")]
        float sampleStep = 0.5f;

        public float HitRadius => hitRadius;
        public float SampleStep => sampleStep;
    }
}
