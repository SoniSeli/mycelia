using System;

namespace Mycelia.Game
{
    /// <summary>What the settings switches show (design §8.3 slice 5): sound effects, music and haptics, each on or off.</summary>
    public readonly struct SettingsChoice : IEquatable<SettingsChoice>
    {
        public readonly bool Effects;
        public readonly bool Music;
        public readonly bool Haptics;

        public SettingsChoice(bool effects, bool music, bool haptics)
        {
            Effects = effects;
            Music = music;
            Haptics = haptics;
        }

        public bool Equals(SettingsChoice other) => Effects == other.Effects && Music == other.Music && Haptics == other.Haptics;

        public override bool Equals(object obj) => obj is SettingsChoice other && Equals(other);

        public override int GetHashCode() => (Effects, Music, Haptics).GetHashCode();

        public override string ToString() => $"effects {Effects}, music {Music}, haptics {Haptics}";
    }
}
