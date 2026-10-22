namespace Mycelia.Domain.Chains
{
    /// <summary>What a <see cref="Chain.Touch"/> did, so the view can react (line, sound, haptics).</summary>
    public enum ChainChange
    {
        /// <summary>The touched cell cannot join the chain; nothing changed.</summary>
        Rejected,

        /// <summary>The finger is still on the last cell; nothing changed.</summary>
        Unchanged,

        /// <summary>The first spore was selected.</summary>
        Started,

        /// <summary>A spore was added to the end of the chain.</summary>
        Extended,

        /// <summary>The finger moved back onto the second-to-last spore; the last one was removed.</summary>
        Backtracked
    }
}
