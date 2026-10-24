using System;
using System.Collections.Generic;
using Mycelia.Domain.Levels;

namespace Mycelia.App
{
    /// <summary>The player's settings (ADR-0006). Used from Milestone 4; saved from the first version.</summary>
    public readonly struct GameSettings : IEquatable<GameSettings>
    {
        public static readonly GameSettings Default = new GameSettings(sfxVolume: 0.8f, musicVolume: 0.6f, haptics: true);

        public readonly float SfxVolume;
        public readonly float MusicVolume;
        public readonly bool Haptics;

        public GameSettings(float sfxVolume, float musicVolume, bool haptics)
        {
            SfxVolume = sfxVolume;
            MusicVolume = musicVolume;
            Haptics = haptics;
        }

        public bool Equals(GameSettings other) =>
            SfxVolume.Equals(other.SfxVolume) && MusicVolume.Equals(other.MusicVolume) && Haptics == other.Haptics;

        public override bool Equals(object obj) => obj is GameSettings other && Equals(other);

        public override int GetHashCode() => (SfxVolume, MusicVolume, Haptics).GetHashCode();

        public override string ToString() => $"sfx {SfxVolume}, music {MusicVolume}, haptics {(Haptics ? "on" : "off")}";
    }

    /// <summary>
    /// Everything saved between sessions, independent of the file version that held it (ADR-0006). Level records
    /// become a <see cref="Progression"/> once the level order is known.
    /// </summary>
    public sealed class SavedProgress
    {
        public static readonly SavedProgress Empty = new SavedProgress(Array.Empty<LevelRecord>(), GameSettings.Default, introSeen: false);

        public SavedProgress(IReadOnlyList<LevelRecord> levels, GameSettings settings, bool introSeen)
        {
            Levels = levels ?? throw new ArgumentNullException(nameof(levels));
            Settings = settings;
            IntroSeen = introSeen;
        }

        public IReadOnlyList<LevelRecord> Levels { get; }
        public GameSettings Settings { get; }
        public bool IntroSeen { get; }
    }
}
