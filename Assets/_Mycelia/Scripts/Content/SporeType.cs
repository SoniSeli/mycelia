using UnityEngine;

namespace Mycelia.Content
{
    /// <summary>
    /// One kind of spore: the source of truth for its identity and look (ADR-0005).
    /// The domain only sees the runtime id that <see cref="SporeCatalog"/> assigns.
    /// </summary>
    [CreateAssetMenu(menuName = "Mycelia/Spore Type", fileName = "SporeType")]
    public sealed class SporeType : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id for analytics and saves: lowercase letters, digits and hyphens. Never change it once shipped.")]
        string key;

        [SerializeField, Tooltip("The species name shown to players.")]
        string displayName;

        [SerializeField]
        Color color = Color.white;

        [SerializeField, Tooltip("A white silhouette, tinted with Color. Every type needs its own shape (colour-blind support).")]
        Sprite sprite;

        [SerializeField, Tooltip("Read by screen readers and used in accessibility labels.")]
        string accessibleLabel;

        public string Key => key;
        public string DisplayName => displayName;
        public Color Color => color;
        public Sprite Sprite => sprite;
        public string AccessibleLabel => accessibleLabel;

        internal void Set(string key, string displayName, Color color, Sprite sprite, string accessibleLabel)
        {
            this.key = key;
            this.displayName = displayName;
            this.color = color;
            this.sprite = sprite;
            this.accessibleLabel = accessibleLabel;
        }
    }
}
