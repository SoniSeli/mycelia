using System;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;

namespace Mycelia.Game
{
    /// <summary>
    /// Sits between chain input and the chain view, and between chain input and the move player, so the chain is felt
    /// (design §8.3 slice 9): a tick for every spore that joins the chain or is dragged back off it, nothing for a touch
    /// on one that can't join, and a stronger one when the chain is committed. Everything is passed on unchanged.
    /// Allocation-free.
    /// </summary>
    public sealed class ChainHaptics : IChainFeedback, IMovePlayer
    {
        readonly IHaptics haptics;
        readonly IChainFeedback feedback;
        readonly IMovePlayer player;

        public ChainHaptics(IHaptics haptics, IChainFeedback feedback, IMovePlayer player)
        {
            this.haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));
            this.feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public bool IsPlaying => player.IsPlaying;

        public void ChainChanged(Chain chain, ChainChange change, GridPosition cell)
        {
            if (change == ChainChange.Started || change == ChainChange.Extended || change == ChainChange.Backtracked)
                haptics.Play(Haptic.Tick);
            feedback.ChainChanged(chain, change, cell);
        }

        public void ChainEnded(Chain chain) => feedback.ChainEnded(chain);

        public void Play(MoveResult move)
        {
            haptics.Play(Haptic.Commit);
            player.Play(move);
        }
    }
}
