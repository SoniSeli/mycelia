using System;
using Mycelia.Domain.Moves;

namespace Mycelia.Domain.Objectives
{
    /// <summary>Collect <see cref="Target"/> spores of one type; progress stops at the target.</summary>
    public sealed class CollectSporesObjective : IObjective
    {
        public SporeTypeId Type { get; }
        public int Target { get; }
        public int Current { get; private set; }
        public bool IsComplete => Current >= Target;

        public CollectSporesObjective(SporeTypeId type, int target)
        {
            if (type == SporeTypeId.None)
                throw new ArgumentException("An objective needs a spore type.", nameof(type));
            if (target <= 0)
                throw new ArgumentOutOfRangeException(nameof(target), target, "Must be greater than zero.");

            Type = type;
            Target = target;
        }

        public bool Apply(in SporeCleared cleared)
        {
            if (cleared.Type != Type || IsComplete)
                return false;

            Current++;
            return true;
        }
    }
}
