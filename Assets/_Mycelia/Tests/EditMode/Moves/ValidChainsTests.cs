using NUnit.Framework;
using Mycelia.Domain.Moves;

namespace Mycelia.Domain.Tests.Moves
{
    public class ValidChainsTests
    {
        [Test]
        public void Exists_ForAnOrthogonalPair()
        {
            Assert.That(ValidChains.Exists(Boards.Parse(@"
                A B
                C A
                D A")), Is.True);
        }

        [Test]
        public void Exists_ForAnUpRightDiagonalPair()
        {
            Assert.That(ValidChains.Exists(Boards.Parse(@"
                B A
                A C")), Is.True);
        }

        [Test]
        public void Exists_ForAnUpLeftDiagonalPair()
        {
            Assert.That(ValidChains.Exists(Boards.Parse(@"
                A B
                C A")), Is.True);
        }

        [Test]
        public void DoesNotExist_WhenNoTwoNeighboursShareAType()
        {
            Assert.That(ValidChains.Exists(Boards.Parse(@"
                A B A B
                C D C D
                A B A B")), Is.False);
        }

        [Test]
        public void DoesNotExist_ForEmptyCells()
        {
            Assert.That(ValidChains.Exists(Boards.Parse(@"
                . .
                . .")), Is.False);
        }

        [Test]
        public void DoesNotExist_AcrossAVoidCell()
        {
            Assert.That(ValidChains.Exists(Boards.Parse("A # A")), Is.False);
        }
    }
}
