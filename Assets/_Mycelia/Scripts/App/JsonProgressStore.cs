using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mycelia.Domain.Levels;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// Keeps progress in <c>progress.json</c> (ADR-0006). Writes are atomic: a temporary file replaces the main one,
    /// and the previous main file becomes <c>.bak</c>. Loading tries the main file, then the backup, then starts
    /// empty; an unreadable main file is moved to <c>.corrupt</c> first, so the next save can't push it over the good
    /// backup. A file from a newer version is never overwritten.
    /// </summary>
    public sealed class JsonProgressStore : IProgressStore
    {
        public const string FileName = "progress.json";
        const int CurrentVersion = ProgressDataV1.Version;

        enum ReadKind { Missing, Unreadable, Newer, Ok }

        readonly string directory;
        readonly string mainPath;
        readonly string backupPath;
        readonly string tempPath;
        readonly string corruptPath;

        // Set when the file on disk is from a newer version: this session must not replace it.
        bool readOnly;

        public JsonProgressStore(string directory)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            mainPath = Path.Combine(directory, FileName);
            backupPath = mainPath + ".bak";
            tempPath = mainPath + ".tmp";
            corruptPath = mainPath + ".corrupt";
        }

        public ProgressLoad Load()
        {
            readOnly = false;

            ReadKind main = Read(mainPath, out ProgressDataV1 data);
            if (main == ReadKind.Ok)
                return ToLoad(data, LoadOutcome.Loaded);
            if (main == ReadKind.Newer)
                return Newer();
            if (main == ReadKind.Unreadable)
                SetAside();

            ReadKind backup = Read(backupPath, out data);
            if (backup == ReadKind.Ok)
            {
                Debug.LogWarning($"[Mycelia] {FileName} was {(main == ReadKind.Missing ? "missing" : "unreadable")}; progress was loaded from the backup.");
                return ToLoad(data, LoadOutcome.RecoveredFromBackup);
            }
            if (backup == ReadKind.Newer)
                return Newer();
            if (main == ReadKind.Missing && backup == ReadKind.Missing)
                return new ProgressLoad(SavedProgress.Empty, LoadOutcome.NoSave, 0);

            Debug.LogWarning("[Mycelia] No readable save was found; progress starts over.");
            return new ProgressLoad(SavedProgress.Empty, LoadOutcome.Reset, 0);
        }

        public bool Save(SavedProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (readOnly)
            {
                Debug.LogWarning($"[Mycelia] {FileName} is from a newer version of the game; it is left unchanged.");
                return false;
            }

            string json = JsonUtility.ToJson(ToData(progress));
            try
            {
                Directory.CreateDirectory(directory);
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(mainPath))
                    File.Replace(tempPath, mainPath, backupPath, ignoreMetadataErrors: true);
                else
                    File.Move(tempPath, mainPath);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Mycelia] Progress could not be saved: {e.Message}");
                return false;
            }
        }

        ProgressLoad Newer()
        {
            readOnly = true;
            Debug.LogWarning($"[Mycelia] {FileName} is from a newer version of the game; running on defaults without saving.");
            return new ProgressLoad(SavedProgress.Empty, LoadOutcome.NewerVersion, 0);
        }

        static ReadKind Read(string path, out ProgressDataV1 data)
        {
            data = null;
            if (!File.Exists(path))
                return ReadKind.Missing;

            try
            {
                string json = File.ReadAllText(path);
                var probe = JsonUtility.FromJson<ProgressVersionProbe>(json);
                if (probe == null || probe.version < 1)
                    return ReadKind.Unreadable;
                if (probe.version > CurrentVersion)
                    return ReadKind.Newer;

                data = Migrate(probe.version, json);
                return data != null ? ReadKind.Ok : ReadKind.Unreadable;
            }
            catch (Exception)
            {
                // JsonUtility reports bad JSON as ArgumentException; whatever the cause, a bad file must not crash the game.
                return ReadKind.Unreadable;
            }
        }

        // Reads a file of any shipped version into its own class, then migrates one version at a time to the current one.
        static ProgressDataV1 Migrate(int version, string json)
        {
            switch (version)
            {
                case 1: return JsonUtility.FromJson<ProgressDataV1>(json);
                default: return null;
            }
        }

        void SetAside()
        {
            try
            {
                if (File.Exists(corruptPath)) File.Delete(corruptPath);
                File.Move(mainPath, corruptPath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Mycelia] The unreadable {FileName} could not be set aside: {e.Message}");
            }
        }

        // Drops records that can't be right (bad stars, empty key); a repeated key keeps its highest stars.
        static ProgressLoad ToLoad(ProgressDataV1 data, LoadOutcome outcome)
        {
            var bestStars = new Dictionary<string, int>();
            var order = new List<string>();
            int dropped = 0;

            foreach (LevelRecordDataV1 record in data.levels ?? new List<LevelRecordDataV1>())
            {
                if (record == null || string.IsNullOrEmpty(record.key) || record.bestStars < 1 || record.bestStars > 3)
                {
                    dropped++;
                    continue;
                }

                if (bestStars.TryGetValue(record.key, out int stars))
                {
                    dropped++;
                    if (record.bestStars > stars) bestStars[record.key] = record.bestStars;
                    continue;
                }

                bestStars.Add(record.key, record.bestStars);
                order.Add(record.key);
            }

            if (dropped > 0)
                Debug.LogWarning($"[Mycelia] {dropped} invalid level record(s) in the save were dropped.");

            var records = new LevelRecord[order.Count];
            for (int i = 0; i < order.Count; i++)
            {
                records[i] = new LevelRecord(order[i], bestStars[order[i]]);
            }

            SettingsDataV1 settings = data.settings ?? new SettingsDataV1();
            var progress = new SavedProgress(
                records,
                new GameSettings(Mathf.Clamp01(settings.sfxVolume), Mathf.Clamp01(settings.musicVolume), settings.haptics),
                data.introSeen);
            return new ProgressLoad(progress, outcome, dropped);
        }

        static ProgressDataV1 ToData(SavedProgress progress)
        {
            var data = new ProgressDataV1
            {
                settings = new SettingsDataV1
                {
                    sfxVolume = progress.Settings.SfxVolume,
                    musicVolume = progress.Settings.MusicVolume,
                    haptics = progress.Settings.Haptics,
                },
                introSeen = progress.IntroSeen,
            };
            foreach (LevelRecord record in progress.Levels)
            {
                data.levels.Add(new LevelRecordDataV1 { key = record.Key, bestStars = record.BestStars });
            }
            return data;
        }
    }
}
