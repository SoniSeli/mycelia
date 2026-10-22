using System;
using NUnit.Framework;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Tests.Sessions
{
    public class MoveBudgetTests
    {
        [Test]
        public void Spend_UsesOneMove()
        {
            var budget = new MoveBudget(20);

            budget.Spend();

            Assert.That(budget.Remaining, Is.EqualTo(19));
            Assert.That(budget.Starting, Is.EqualTo(20));
        }

        [Test]
        public void SpendingTheLastMove_ExhaustsTheBudget()
        {
            var budget = new MoveBudget(1);

            budget.Spend();

            Assert.That(budget.IsExhausted, Is.True);
        }

        [Test]
        public void Spend_WithNoMovesLeft_Throws()
        {
            var budget = new MoveBudget(1);
            budget.Spend();

            Assert.Throws<InvalidOperationException>(() => budget.Spend());
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsANonPositiveBudget(int moves)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoveBudget(moves));
        }
    }
}
