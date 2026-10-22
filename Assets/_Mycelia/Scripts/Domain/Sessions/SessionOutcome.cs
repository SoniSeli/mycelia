using System.Collections.Generic;
using Mycelia.Domain.Objectives;

namespace Mycelia.Domain.Sessions
{
    public enum SessionState
    {
        Playing,
        Won,
        Lost
    }

    /// <summary>Design §3.5: win when every objective is complete; lose when moves run out first. The win is checked first.</summary>
    public static class SessionOutcome
    {
        public static SessionState Evaluate(IReadOnlyList<IObjective> objectives, MoveBudget moves)
        {
            if (AllComplete(objectives))
                return SessionState.Won;

            return moves.IsExhausted ? SessionState.Lost : SessionState.Playing;
        }

        static bool AllComplete(IReadOnlyList<IObjective> objectives)
        {
            for (int i = 0; i < objectives.Count; i++)
            {
                if (!objectives[i].IsComplete)
                    return false;
            }
            return true;
        }
    }
}
