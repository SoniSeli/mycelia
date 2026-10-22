using System;

namespace Mycelia.Domain
{
    /// <summary>
    /// Runtime identity of a spore type (ADR-0005). Compact and allocation-free; never persisted.
    /// <see cref="None"/> (0) marks an empty cell.
    /// </summary>
    public readonly struct SporeTypeId : IEquatable<SporeTypeId>
    {
        public static readonly SporeTypeId None = default;

        public readonly byte Value;

        public SporeTypeId(byte value) => Value = value;

        public bool Equals(SporeTypeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SporeTypeId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => this == None ? "None" : $"Spore#{Value}";

        public static bool operator ==(SporeTypeId left, SporeTypeId right) => left.Equals(right);
        public static bool operator !=(SporeTypeId left, SporeTypeId right) => !left.Equals(right);
    }
}
