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
        static LevelSession NewSession(ulong seed)
        {
            var types = "ABCD".Select(Boards.IdOf).ToArray();
            var config = new LevelConfig("level-test", 6, 6, 1000, types,
                new[] { new ObjectiveDefinition(types[0], 10_000) });
            return new LevelSession(config, new Pcg32(seed));
        }

        [Test]
        public void Commit_DoesNotAllocateAfterWarmUp()
        {
            var session = NewSession(seed: 123);

            for (int i = 0; i < 5; i++)
            {
                MakeAMove(session);
            }

            var (first, second) = MoveResultExtensions.FindPair(session.Board);
            session.Chain.Touch(first);
            session.Chain.Touch(second);

            Assert.That(() => { session.Commit(); }, Is.Not.AllocatingGCMemory());
        }

        // Found on the phone: a cost that comes back with every level allocates mid-play, on its first move.
        [Test]
        public void TheFirstMoveOfANewLevel_DoesNotAllocate()
        {
            // Another session warms up what every session shares (statics, comparers), as earlier levels would.
            var earlier = NewSession(seed: 123);
            for (int i = 0; i < 5; i++)
            {
                MakeAMove(earlier);
            }

            var session = NewSession(seed: 456);
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
