using NUnit.Framework;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Tests.Chains
{
    public class ChainTests
    {
        // (0,0) is the bottom-left B.
        const string Board3x3 = @"
            A A B
            A B A
            B A A";

        Chain chain;

        [SetUp]
        public void SetUp() => chain = new Chain(Boards.Parse(Board3x3));

        static GridPosition At(int column, int row) => new GridPosition(column, row);

        [Test]
        public void Touch_OnASpore_StartsTheChainWithItsType()
        {
            Assert.That(chain.Touch(At(1, 0)), Is.EqualTo(ChainChange.Started));
            Assert.That(chain.Cells, Is.EqualTo(new[] { At(1, 0) }));
            Assert.That(chain.Type, Is.EqualTo(Boards.IdOf('A')));
        }

        [Test]
        public void Touch_OnAnEmptyCell_IsRejected()
        {
            var withGap = new Chain(Boards.Parse("A ."));

            Assert.That(withGap.Touch(At(1, 0)), Is.EqualTo(ChainChange.Rejected));
            Assert.That(withGap.Cells, Is.Empty);
        }

        [Test]
        public void Touch_OnAVoidCell_IsRejected()
        {
            var withVoid = new Chain(Boards.Parse("A #"));

            Assert.That(withVoid.Touch(At(1, 0)), Is.EqualTo(ChainChange.Rejected));
        }

        [Test]
        public void Touch_OutsideTheBoard_IsRejected()
        {
            Assert.That(chain.Touch(At(3, 0)), Is.EqualTo(ChainChange.Rejected));

            chain.Touch(At(2, 0));
            Assert.That(chain.Touch(At(3, 0)), Is.EqualTo(ChainChange.Rejected));
        }

        [Test]
        public void Touch_OnAnOrthogonalNeighbourOfTheSameType_ExtendsTheChain()
        {
            chain.Touch(At(1, 0));

            Assert.That(chain.Touch(At(2, 0)), Is.EqualTo(ChainChange.Extended));
            Assert.That(chain.Cells, Is.EqualTo(new[] { At(1, 0), At(2, 0) }));
        }

        [Test]
        public void Touch_OnADiagonalNeighbourOfTheSameType_ExtendsTheChain()
        {
            chain.Touch(At(1, 0));

            Assert.That(chain.Touch(At(0, 1)), Is.EqualTo(ChainChange.Extended));
        }

        [Test]
        public void Touch_OnANeighbourOfADifferentType_IsRejected()
        {
            chain.Touch(At(1, 0));

            Assert.That(chain.Touch(At(1, 1)), Is.EqualTo(ChainChange.Rejected));
            Assert.That(chain.Cells, Is.EqualTo(new[] { At(1, 0) }));
        }

        [Test]
        public void Touch_OnASameTypeCellThatIsNotANeighbour_IsRejected()
        {
            chain.Touch(At(1, 0));

            Assert.That(chain.Touch(At(1, 2)), Is.EqualTo(ChainChange.Rejected));
        }

        [Test]
        public void Touch_OnACellAlreadyInTheChain_IsRejected()
        {
            chain.Touch(At(1, 0));
            chain.Touch(At(2, 0));
            chain.Touch(At(2, 1));

            // (1,0) is a diagonal neighbour of (2,1) and the same type, but it is already in the chain.
            Assert.That(chain.Touch(At(1, 0)), Is.EqualTo(ChainChange.Rejected));
            Assert.That(chain.Count, Is.EqualTo(3));
        }

        [Test]
        public void Touch_OnTheSecondToLastCell_BacktracksOneStep()
        {
            chain.Touch(At(1, 0));
            chain.Touch(At(2, 0));
            chain.Touch(At(2, 1));

            Assert.That(chain.Touch(At(2, 0)), Is.EqualTo(ChainChange.Backtracked));
            Assert.That(chain.Cells, Is.EqualTo(new[] { At(1, 0), At(2, 0) }));
        }

        [Test]
        public void Backtracking_CanUnwindTheChainBackToItsFirstCell()
        {
            chain.Touch(At(1, 0));
            chain.Touch(At(2, 0));
            chain.Touch(At(2, 1));

            chain.Touch(At(2, 0));
            Assert.That(chain.Touch(At(1, 0)), Is.EqualTo(ChainChange.Backtracked));
            Assert.That(chain.Cells, Is.EqualTo(new[] { At(1, 0) }));
        }

        [Test]
        public void Touch_OnTheLastCellAgain_LeavesTheChainUnchanged()
        {
            chain.Touch(At(1, 0));
            chain.Touch(At(2, 0));

            Assert.That(chain.Touch(At(2, 0)), Is.EqualTo(ChainChange.Unchanged));
            Assert.That(chain.Count, Is.EqualTo(2));
        }

        [Test]
        public void IsCommittable_RequiresAtLeastTwoSpores()
        {
            // Design §3.2: a single spore is cancelled and costs no move.
            Assert.That(chain.IsCommittable, Is.False);

            chain.Touch(At(1, 0));
            Assert.That(chain.IsCommittable, Is.False);

            chain.Touch(At(2, 0));
            Assert.That(chain.IsCommittable, Is.True);
        }

        [Test]
        public void CanExtend_AnswersWithoutChangingTheChain()
        {
            chain.Touch(At(1, 0));

            Assert.That(chain.CanExtend(At(2, 0)), Is.True);
            Assert.That(chain.CanExtend(At(1, 1)), Is.False);
            Assert.That(chain.Cells, Is.EqualTo(new[] { At(1, 0) }));
        }

        [Test]
        public void CanExtend_IsFalseForAnEmptyChain()
        {
            Assert.That(chain.CanExtend(At(1, 0)), Is.False);
        }

        [Test]
        public void Clear_EmptiesTheChain()
        {
            chain.Touch(At(1, 0));
            chain.Touch(At(2, 0));

            chain.Clear();

            Assert.That(chain.Cells, Is.Empty);
            Assert.That(chain.Type, Is.EqualTo(SporeTypeId.None));
            Assert.That(chain.Touch(At(0, 0)), Is.EqualTo(ChainChange.Started));
        }
    }
}
