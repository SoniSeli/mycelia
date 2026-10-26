using System;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;

namespace Mycelia.Game
{
    /// <summary>
    /// Sits between chain input and the chain's other listeners, and between chain input and the move player, so the chain
    /// is heard (design §8.3 slice 10): a tick for every spore that joins the chain or is dragged back off it, pitched by
    /// the chain's length so a growing chain climbs a scale, nothing for a touch on one that can't join, and a pop when the
    /// chain is committed. Everything is passed on unchanged. Allocation-free.
    /// </summary>
    public sealed class ChainSounds : IChainFeedback, IMovePlayer
    {
        readonly ISoundEffects sounds;
        readonly IChainFeedback feedback;
        readonly IMovePlayer player;

        public ChainSounds(ISoundEffects sounds, IChainFeedback feedback, IMovePlayer player)
        {
            this.sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
            this.feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public bool IsPlaying => player.IsPlaying;

        public void ChainChanged(Chain chain, ChainChange change, GridPosition cell)
        {
            if (change == ChainChange.Started || change == ChainChange.Extended || change == ChainChange.Backtracked)
                sounds.PlayTick(chain.Count);
            feedback.ChainChanged(chain, change, cell);
        }

        public void ChainEnded(Chain chain) => feedback.ChainEnded(chain);

        public void Play(MoveResult move)
        {
            sounds.Play(SoundEffect.Commit);
            player.Play(move);
        }
    }
}
