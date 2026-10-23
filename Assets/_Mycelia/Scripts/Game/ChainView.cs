using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Shows the chain being dragged: a line through the chained spores' centres, and those spores drawn slightly
    /// larger so the chain reads without colour. Buffers are sized in <see cref="Init"/>; redrawing allocates nothing.
    /// </summary>
    public sealed class ChainView : MonoBehaviour, IChainFeedback
    {
        /// <summary>The line draws above the spores (<see cref="SporeView.SortingOrder"/>).</summary>
        public const int SortingOrder = 2;

        [SerializeField] LineRenderer line;

        [SerializeField, Range(1f, 1.4f), Tooltip("Scale of a chained spore.")]
        float chainedScale = 1.15f;

        BoardView boardView;
        SporeView[] emphasised;
        int emphasisedCount;

        public void Init(BoardView boardView, int cells)
        {
            this.boardView = boardView;
            emphasised = new SporeView[cells];
            line.useWorldSpace = false;
            line.sortingOrder = SortingOrder;
            line.positionCount = 0;
        }

        public void ChainChanged(Chain chain, ChainChange change, GridPosition cell)
        {
            if (change != ChainChange.Rejected)
                Redraw(chain);
        }

        public void ChainEnded(Chain chain) => Redraw(chain);

        void Redraw(Chain chain)
        {
            for (int i = 0; i < emphasisedCount; i++)
                emphasised[i].transform.localScale = Vector3.one;
            emphasisedCount = 0;

            line.positionCount = chain.Count;
            for (int i = 0; i < chain.Count; i++)
            {
                GridPosition cell = chain.Cells[i];
                line.SetPosition(i, BoardLayout.CellCentre(cell));

                SporeView spore = boardView.SporeAt(cell);
                if (spore == null) continue;
                spore.transform.localScale = new Vector3(chainedScale, chainedScale, 1f);
                emphasised[emphasisedCount++] = spore;
            }
        }
    }
}
