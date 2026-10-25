using Mycelia.Content;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>One spore on the board. Views are pooled and reused for whichever spore needs one (ADR-0011).</summary>
    public sealed class SporeView : MonoBehaviour
    {
        /// <summary>Spores draw above the backdrop (<see cref="Backdrop.SporeSortingOrder"/>) and the chain line.</summary>
        public const int SortingOrder = 1;

        [SerializeField] SpriteRenderer spriteRenderer;

        public SpriteRenderer Renderer => spriteRenderer;

        internal bool InPool { get; set; }

        public void Show(SporeType type, Vector2 position)
        {
            spriteRenderer.sprite = type.Sprite;
            spriteRenderer.color = type.Color;
            transform.localPosition = position;
            transform.localScale = Vector3.one;
        }

        // One-time setup when the pool creates the view. Reading transform creates its C# wrapper now, while the
        // level loads, instead of the first time a move shows the view (0 B per move).
        internal void Prepare()
        {
            spriteRenderer.sortingOrder = SortingOrder;
            _ = transform;
        }
    }
}
