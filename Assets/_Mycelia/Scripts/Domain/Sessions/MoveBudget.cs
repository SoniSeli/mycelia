using System;

namespace Mycelia.Domain.Sessions
{
    /// <summary>The moves a level allows; each committed chain spends one (design §3.3 step 4).</summary>
    public sealed class MoveBudget
    {
        public int Starting { get; }
        public int Remaining { get; private set; }
        public bool IsExhausted => Remaining == 0;

        public MoveBudget(int moves)
        {
            if (moves <= 0)
                throw new ArgumentOutOfRangeException(nameof(moves), moves, "Must be greater than zero.");

            Starting = moves;
            Remaining = moves;
        }

        public void Spend()
        {
            if (IsExhausted)
                throw new InvalidOperationException("No moves left to spend.");

            Remaining--;
        }
    }
}
