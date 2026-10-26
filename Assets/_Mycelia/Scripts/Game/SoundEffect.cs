namespace Mycelia.Game
{
    /// <summary>The moments the player hears (design §8.3 slice 10): the haptics' moments, and button taps.</summary>
    public enum SoundEffect
    {
        /// <summary>A spore joined the chain or was dragged back off it: one note, a step higher for every spore in the chain.</summary>
        Tick,

        /// <summary>A chain was committed as a move: a soft bubble pop.</summary>
        Commit,

        /// <summary>The level was won, as the win wave starts.</summary>
        Win,

        /// <summary>The level was lost, as the loss card shows.</summary>
        Loss,

        /// <summary>A button was tapped.</summary>
        Tap
    }
}
