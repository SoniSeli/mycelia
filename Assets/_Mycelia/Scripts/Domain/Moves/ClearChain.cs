using System.Collections.Generic;
using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Moves
{
    /// <summary>Design §3.3 step 1: the chained spores are removed.</summary>
    public static class ClearChain
    {
        public static void Apply(Board board, IReadOnlyList<GridPosition> chain, List<SporeCleared> cleared)
        {
            for (int i = 0; i < chain.Count; i++)
            {
                GridPosition cell = chain[i];
                cleared.Add(new SporeCleared(cell, board.GetOccupant(cell)));
                board.SetOccupant(cell, SporeTypeId.None);
            }
        }
    }
}
