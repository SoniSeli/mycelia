using Mycelia.Domain.Grid;

namespace Mycelia.Domain.Moves
{
    /// <summary>Marker for the small structs that describe one step of a move, for playback and tests (ADR-0004).</summary>
    public interface IMoveStep { }

    /// <summary>A spore was removed by the player's chain.</summary>
    public readonly struct SporeCleared : IMoveStep
    {
        public readonly GridPosition Cell;
        public readonly SporeTypeId Type;

        public SporeCleared(GridPosition cell, SporeTypeId type)
        {
            Cell = cell;
            Type = type;
        }

        public override string ToString() => $"Cleared {Type} at {Cell}";
    }

    /// <summary>A spore fell straight down to fill a gap.</summary>
    public readonly struct SporeFell : IMoveStep
    {
        public readonly GridPosition From;
        public readonly GridPosition To;

        public SporeFell(GridPosition from, GridPosition to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => $"Fell {From} -> {To}";
    }

    /// <summary>A spore was moved to another cell by a reshuffle.</summary>
    public readonly struct SporeRelocated : IMoveStep
    {
        public readonly GridPosition From;
        public readonly GridPosition To;

        public SporeRelocated(GridPosition from, GridPosition to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => $"Relocated {From} -> {To}";
    }

    /// <summary>A new spore appeared in an empty cell. <see cref="DropSlot"/> counts from the lowest new spore in its column.</summary>
    public readonly struct SporeSpawned : IMoveStep
    {
        public readonly GridPosition Cell;
        public readonly SporeTypeId Type;
        public readonly int DropSlot;

        public SporeSpawned(GridPosition cell, SporeTypeId type, int dropSlot)
        {
            Cell = cell;
            Type = type;
            DropSlot = dropSlot;
        }

        public override string ToString() => $"Spawned {Type} at {Cell} (slot {DropSlot})";
    }
}
