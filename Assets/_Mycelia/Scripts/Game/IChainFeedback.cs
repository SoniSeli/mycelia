using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;

namespace Mycelia.Game
{
    /// <summary>Reacts to the chain as it is dragged: the line and spore emphasis now, sound and haptics later (ADR-0008).</summary>
    public interface IChainFeedback
    {
        /// <summary>Called for every cell the finger touches, including <see cref="ChainChange.Rejected"/> ones.</summary>
        void ChainChanged(Chain chain, ChainChange change, GridPosition cell);

        /// <summary>Called on release, after the chain was committed or cancelled; the chain is empty again.</summary>
        void ChainEnded(Chain chain);
    }
}
