using System;
using System.Collections.Generic;

namespace Mycelia.App
{
    // The JsonUtility shapes of progress.json (ADR-0006). One class per shipped version, kept forever: a file of any
    // shipped version is read into its own class and migrated one version at a time. Field names are the file format.

    [Serializable]
    sealed class ProgressVersionProbe
    {
        public int version;
    }

    [Serializable]
    sealed class ProgressDataV1
    {
        public const int Version = 1;

        public int version = Version;
        public List<LevelRecordDataV1> levels = new List<LevelRecordDataV1>();
        public SettingsDataV1 settings = new SettingsDataV1();
        public bool introSeen;
    }

    [Serializable]
    sealed class LevelRecordDataV1
    {
        public string key;
        public int bestStars;
    }

    [Serializable]
    sealed class SettingsDataV1
    {
        public float sfxVolume = GameSettings.Default.SfxVolume;
        public float musicVolume = GameSettings.Default.MusicVolume;
        public bool haptics = GameSettings.Default.Haptics;
    }
}
