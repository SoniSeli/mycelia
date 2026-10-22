using System;
using NUnit.Framework;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Moves;
using Mycelia.Domain.Objectives;

namespace Mycelia.Domain.Tests.Objectives
{
    public class CollectSporesObjectiveTests
    {
        static readonly SporeTypeId A = Boards.IdOf('A');
        static readonly SporeTypeId B = Boards.IdOf('B');

        static SporeCleared Cleared(SporeTypeId type) => new SporeCleared(new GridPosition(0, 0), type);

        [Test]
        public void ClearingTheTargetType_CountsTowardTheObjective()
        {
            var objective = new CollectSporesObjective(A, target: 3);

            bool changed = objective.Apply(Cleared(A));

            Assert.That(changed, Is.True);
            Assert.That(objective.Current, Is.EqualTo(1));
            Assert.That(objective.IsComplete, Is.False);
        }

        [Test]
        public void ClearingAnotherType_DoesNotCount()
        {
            var objective = new CollectSporesObjective(A, target: 3);

            bool changed = objective.Apply(Cleared(B));

            Assert.That(changed, Is.False);
            Assert.That(objective.Current, Is.Zero);
        }

        [Test]
        public void Progress_IsCappedAtTheTarget()
        {
            // Design §3.5: clearing spores of type T counts toward the objective, capped at its target.
            var objective = new CollectSporesObjective(A, target: 2);

            objective.Apply(Cleared(A));
            objective.Apply(Cleared(A));
            bool changed = objective.Apply(Cleared(A));

            Assert.That(changed, Is.False);
            Assert.That(objective.Current, Is.EqualTo(2));
            Assert.That(objective.IsComplete, Is.True);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Constructor_RejectsANonPositiveTarget(int target)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CollectSporesObjective(A, target));
        }

        [Test]
        public void Constructor_RejectsTheEmptyType()
        {
            Assert.Throws<ArgumentException>(() => new CollectSporesObjective(SporeTypeId.None, target: 1));
        }
    }
}
