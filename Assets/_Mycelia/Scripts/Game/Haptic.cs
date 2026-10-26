namespace Mycelia.Game
{
    /// <summary>The moments the player feels (design §8.3 slice 9); the platform picks each one's strength (ADR-0016).</summary>
    public enum Haptic
    {
        /// <summary>The lightest: a spore joined the chain or was dragged back off it.</summary>
        Tick,

        /// <summary>A chain was committed as a move.</summary>
        Commit,

        /// <summary>The level was won, as the win wave starts.</summary>
        Win,

        /// <summary>The level was lost, as the loss card shows.</summary>
        Loss
    }
}
