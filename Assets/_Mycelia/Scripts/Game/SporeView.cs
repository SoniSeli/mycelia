using Mycelia.Content;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>One spore on the board. Views are pooled and reused for whichever spore needs one (ADR-0011).</summary>
    public sealed class SporeView : MonoBehaviour
    {
        /// <summary>Spores draw above the cell tiles (<see cref="BoardView.CellSortingOrder"/>).</summary>
        public const int SortingOrder = 1;

        [SerializeField] SpriteRenderer spriteRenderer;

        public SpriteRenderer Renderer => spriteRenderer;

        internal bool InPool { get; set; }

        public void Show(SporeType type, Vector2 position)
        {
            spriteRenderer.sprite = type.Sprite;
            spriteRenderer.color = type.Color;
            transform.localPosition = position;
        }

        // One-time setup when the pool creates the view.
        internal void Prepare() => spriteRenderer.sortingOrder = SortingOrder;
    }
}
