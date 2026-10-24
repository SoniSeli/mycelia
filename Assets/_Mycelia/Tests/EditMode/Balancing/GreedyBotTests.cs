using System.Linq;
using Mycelia.Balancing;
using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using NUnit.Framework;

namespace Mycelia.Domain.Tests.Balancing
{
    public class GreedyBotTests
    {
        static LevelSession Session(string grid, char goal, int target, int moves = 10)
        {
            Board board = Boards.Parse(grid);
            var config = new LevelConfig(
                "level-test", board.Width, board.Height, moves,
                new[] { Boards.IdOf('A'), Boards.IdOf('B'), Boards.IdOf('C'), Boards.IdOf('D') },
                new[] { new ObjectiveDefinition(Boards.IdOf(goal), target) });
            return new LevelSession(config, new Pcg32(1), board);
        }

        static char[] TypesOf(LevelSession session, GridPosition[] path) =>
            path.Select(cell => (char)('A' + session.Board.GetOccupant(cell).Value - 1)).ToArray();

        [Test]
        public void TheBot_ChainsTheGoalsType_OverALongerChainOfAnother()
        {
            LevelSession session = Session(@"
                B B B B
                C D C D
                A A D C", goal: 'A', target: 5);

            GridPosition[] path = new GreedyBot().ChooseChain(session);

            Assert.That(TypesOf(session, path), Is.EqualTo(new[] { 'A', 'A' }));
        }

        [Test]
        public void AmongGoalTypes_TheBot_TakesTheLongestChain()
        {
            LevelSession session = Session(@"
                A A C D
                C D C D
                A A A D", goal: 'A', target: 10);

            Assert.That(new GreedyBot().ChooseChain(session).Length, Is.EqualTo(3));
        }

        [Test]
        public void TheBot_FollowsASnakingGroup_EndToEnd()
        {
            // Five A spores in an S shape: only a path that starts at an end visits all of them.
            LevelSession session = Session(@"
                A A B
                C A D
                B A A", goal: 'A', target: 10);

            Assert.That(new GreedyBot().ChooseChain(session).Length, Is.EqualTo(5));
        }

        [Test]
        public void WithNoChainOfTheGoalsType_TheBot_PlaysTheLongestChain()
        {
            LevelSession session = Session(@"
                B B B C
                C D A D
                A D C B", goal: 'A', target: 5);

            Assert.That(TypesOf(session, new GreedyBot().ChooseChain(session)), Is.EqualTo(new[] { 'B', 'B', 'B' }));
        }

        [Test]
        public void EveryChosenChain_IsAcceptedLinkByLink_AndCanBeCommitted()
        {
            LevelSession session = Session(@"
                A B A B
                B A B A
                A B A B
                B A B A", goal: 'A', target: 8);

            GridPosition[] path = new GreedyBot().ChooseChain(session);
            foreach (GridPosition cell in path)
                Assert.That(session.Chain.Touch(cell), Is.Not.EqualTo(ChainChange.Rejected), cell.ToString());

            Assert.That(session.Chain.IsCommittable, Is.True);
            // The diagonal A lattice is one group of eight, but its two corner spores each have one neighbour, so
            // the longest path through it visits seven.
            Assert.That(path.Length, Is.EqualTo(7));
        }
    }
}
