using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Objectives;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Tests.Sessions
{
    public class SessionOutcomeTests
    {
        static readonly SporeTypeId A = Boards.IdOf('A');

        static CollectSporesObjective Objective(int target, int collected)
        {
            var objective = new CollectSporesObjective(A, target);
            for (int i = 0; i < collected; i++)
            {
                objective.Apply(new SporeCleared(new GridPosition(0, 0), A));
            }
            return objective;
        }

        static MoveBudget Budget(int starting, int spent)
        {
            var budget = new MoveBudget(starting);
            for (int i = 0; i < spent; i++)
            {
                budget.Spend();
            }
            return budget;
        }

        [Test]
        public void Evaluate_WhenObjectivesAreIncompleteAndMovesRemain_IsPlaying()
        {
            var state = SessionOutcome.Evaluate(new[] { Objective(5, 2) }, Budget(10, 3));

            Assert.That(state, Is.EqualTo(SessionState.Playing));
        }

        [Test]
        public void Evaluate_WhenAllObjectivesAreComplete_IsWon()
        {
            var state = SessionOutcome.Evaluate(new[] { Objective(5, 5), Objective(2, 2) }, Budget(10, 3));

            Assert.That(state, Is.EqualTo(SessionState.Won));
        }

        [Test]
        public void Evaluate_WhenOnlySomeObjectivesAreComplete_IsNotWon()
        {
            var state = SessionOutcome.Evaluate(new[] { Objective(5, 5), Objective(2, 1) }, Budget(10, 3));

            Assert.That(state, Is.EqualTo(SessionState.Playing));
        }

        [Test]
        public void Evaluate_WhenMovesRunOutWithObjectivesIncomplete_IsLost()
        {
            var state = SessionOutcome.Evaluate(new[] { Objective(5, 4) }, Budget(10, 10));

            Assert.That(state, Is.EqualTo(SessionState.Lost));
        }

        [Test]
        public void Evaluate_WhenTheLastMoveCompletesTheObjectives_IsWon()
        {
            // Design §3.5: the win is checked first, so finishing on the final move still wins.
            var state = SessionOutcome.Evaluate(new[] { Objective(5, 5) }, Budget(10, 10));

            Assert.That(state, Is.EqualTo(SessionState.Won));
        }
    }
}
