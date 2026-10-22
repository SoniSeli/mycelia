using Mycelia.Domain.Moves;

namespace Mycelia.Domain.Objectives
{
    /// <summary>A level goal (design §3.5). New objective types implement this interface (ADR-0004).</summary>
    public interface IObjective
    {
        int Current { get; }
        int Target { get; }
        bool IsComplete { get; }

        /// <summary>Applies one spore cleared by the player's chain. Returns true if progress changed.</summary>
        bool Apply(in SporeCleared cleared);
    }
}
