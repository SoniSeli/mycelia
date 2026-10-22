using System.Linq;
using NUnit.Framework;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Domain.Tests.Sessions
{
    /// <summary>Performance budget: 0 B of GC allocation during a move, after warm-up.</summary>
    public class AllocationTests
    {
        [Test]
        public void Commit_DoesNotAllocateAfterWarmUp()
        {
            var types = "ABCD".Select(Boards.IdOf).ToArray();
            var config = new LevelConfig("level-test", 6, 6, 1000, types,
                new[] { new ObjectiveDefinition(types[0], 10_000) });
            var session = new LevelSession(config, new Pcg32(seed: 123));

            for (int i = 0; i < 5; i++)
            {
                MakeAMove(session);
            }

            var (first, second) = MoveResultExtensions.FindPair(session.Board);
            session.Chain.Touch(first);
            session.Chain.Touch(second);

            Assert.That(() => { session.Commit(); }, Is.Not.AllocatingGCMemory());
        }

        static void MakeAMove(LevelSession session)
        {
            var (first, second) = MoveResultExtensions.FindPair(session.Board);
            session.Chain.Touch(first);
            session.Chain.Touch(second);
            session.Commit();
        }
    }
}
