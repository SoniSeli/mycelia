using System;

namespace Mycelia.Game
{
    /// <summary>What a press of Next or back did on the intro.</summary>
    public enum IntroStep
    {
        /// <summary>Another page shows.</summary>
        Turned,
        /// <summary>Next on the last page: the intro is done.</summary>
        Finished,
        /// <summary>Back on the first page: the player left before the end.</summary>
        Left,
    }

    /// <summary>
    /// Which intro page shows (design §8.3 slice 8): Next turns forward and finishes on the last page; back turns back
    /// and leaves from the first. Finishing or leaving stays on the page, as the screen is about to change.
    /// </summary>
    public sealed class IntroPages
    {
        public IntroPages(int count)
        {
            if (count < 1) throw new ArgumentException("The intro needs at least one page.", nameof(count));
            Count = count;
        }

        public int Count { get; }

        public int Current { get; private set; }

        public bool IsLast => Current == Count - 1;

        public IntroStep Next()
        {
            if (IsLast)
                return IntroStep.Finished;

            Current++;
            return IntroStep.Turned;
        }

        public IntroStep Back()
        {
            if (Current == 0)
                return IntroStep.Left;

            Current--;
            return IntroStep.Turned;
        }
    }
}
