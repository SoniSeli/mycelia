using System;

namespace Mycelia.Domain.Sessions
{
    /// <summary>Share of the starting moves that must be left for ★★ and ★★★ (design §3.4).</summary>
    public readonly struct StarThresholds
    {
        public static readonly StarThresholds Default = new StarThresholds(twoStarPercent: 25, threeStarPercent: 50);

        public readonly int TwoStarPercent;
        public readonly int ThreeStarPercent;

        public StarThresholds(int twoStarPercent, int threeStarPercent)
        {
            if (twoStarPercent <= 0 || twoStarPercent > threeStarPercent || threeStarPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(twoStarPercent),
                    $"Need 0 < two-star ({twoStarPercent}) <= three-star ({threeStarPercent}) <= 100.");

            TwoStarPercent = twoStarPercent;
            ThreeStarPercent = threeStarPercent;
        }
    }

    /// <summary>Stars for a won level, from the share of moves left. Integer maths, so boundaries are exact.</summary>
    public static class StarRating
    {
        public static int For(int movesRemaining, int startingMoves, StarThresholds thresholds)
        {
            if (startingMoves <= 0)
                throw new ArgumentOutOfRangeException(nameof(startingMoves), startingMoves, "Must be greater than zero.");
            if (movesRemaining < 0 || movesRemaining > startingMoves)
                throw new ArgumentOutOfRangeException(nameof(movesRemaining), movesRemaining, $"Must be between 0 and {startingMoves}.");

            // remaining / starting >= percent / 100, without floating-point rounding
            long remainingScaled = movesRemaining * 100L;
            if (remainingScaled >= (long)startingMoves * thresholds.ThreeStarPercent) return 3;
            if (remainingScaled >= (long)startingMoves * thresholds.TwoStarPercent) return 2;
            return 1;
        }
    }
}
