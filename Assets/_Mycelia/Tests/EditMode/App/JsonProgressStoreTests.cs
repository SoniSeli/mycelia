using System;
using System.IO;
using System.Linq;
using Mycelia.Domain.Levels;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class JsonProgressStoreTests
    {
        const string GoldenFolder = "Assets/_Mycelia/Tests/EditMode/App/Golden";

        string directory;
        string MainFile => Path.Combine(directory, "progress.json");
        string BackupFile => Path.Combine(directory, "progress.json.bak");
        string CorruptFile => Path.Combine(directory, "progress.json.corrupt");
        string TempFile => Path.Combine(directory, "progress.json.tmp");

        [SetUp]
        public void CreateFolder()
        {
            directory = Path.Combine(Path.GetTempPath(), "mycelia-saves-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }

        JsonProgressStore Store() => new JsonProgressStore(directory);

        static SavedProgress Progress(params (string Key, int Stars)[] levels) =>
            new SavedProgress(levels.Select(l => new LevelRecord(l.Key, l.Stars)).ToArray(), GameSettings.Default, introSeen: false);

        static string V1(string levels, string settings = "{\"sfxVolume\":0.8,\"musicVolume\":0.6,\"haptics\":true}") =>
            "{\"version\":1,\"levels\":[" + levels + "],\"settings\":" + settings + ",\"introSeen\":false}";

        static string Record(string key, int stars) => "{\"key\":\"" + key + "\",\"bestStars\":" + stars + "}";

        static (string, int)[] Levels(ProgressLoad load) => load.Progress.Levels.Select(r => (r.Key, r.BestStars)).ToArray();

        [Test]
        public void WithNoSaveYet_LoadGivesEmptyProgressAndDefaultSettings()
        {
            ProgressLoad load = Store().Load();

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.NoSave));
            Assert.That(load.Progress.Levels, Is.Empty);
            Assert.That(load.Progress.Settings, Is.EqualTo(GameSettings.Default));
            Assert.That(load.Progress.IntroSeen, Is.False);
        }

        [Test]
        public void SavedProgress_LoadsBackUnchanged()
        {
            var settings = new GameSettings(sfxVolume: 0.25f, musicVolume: 0.5f, haptics: false);
            var saved = new SavedProgress(new[] { new LevelRecord("level-01", 3), new LevelRecord("level-02", 1) }, settings, introSeen: true);

            Assert.That(Store().Save(saved), Is.True);
            ProgressLoad load = new JsonProgressStore(directory).Load();

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.Loaded));
            Assert.That(Levels(load), Is.EqualTo(new[] { ("level-01", 3), ("level-02", 1) }));
            Assert.That(load.Progress.Settings, Is.EqualTo(settings));
            Assert.That(load.Progress.IntroSeen, Is.True);
        }

        [Test]
        public void TheSavedFile_IsVersion1()
        {
            Store().Save(Progress(("level-01", 2)));

            Assert.That(File.ReadAllText(MainFile), Does.StartWith("{\"version\":1,"));
        }

        [Test]
        public void Saving_KeepsThePreviousFileAsTheBackup()
        {
            var store = Store();
            store.Save(Progress(("level-01", 1)));
            string first = File.ReadAllText(MainFile);

            store.Save(Progress(("level-01", 2)));

            Assert.That(File.ReadAllText(BackupFile), Is.EqualTo(first));
        }

        [Test]
        public void Saving_LeavesNoTemporaryFile()
        {
            // The first save has no main file to replace; later ones do.
            var store = Store();
            store.Save(Progress(("level-01", 1)));
            Assert.That(File.Exists(TempFile), Is.False, "after the first save");

            store.Save(Progress(("level-01", 2)));
            Assert.That(File.Exists(TempFile), Is.False, "after a later save");
        }

        [Test]
        public void ACorruptFile_FallsBackToTheBackup()
        {
            File.WriteAllText(BackupFile, V1(Record("level-01", 2)));
            File.WriteAllText(MainFile, "{\"version\":1,\"levels\":[{\"key\":\"lev");

            ProgressLoad load = Store().Load();

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.RecoveredFromBackup));
            Assert.That(Levels(load), Is.EqualTo(new[] { ("level-01", 2) }));
        }

        [Test]
        public void AMissingFileWithABackup_LoadsTheBackup()
        {
            File.WriteAllText(BackupFile, V1(Record("level-01", 2)));

            ProgressLoad load = Store().Load();

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.RecoveredFromBackup));
            Assert.That(Levels(load), Is.EqualTo(new[] { ("level-01", 2) }));
        }

        [Test]
        public void ACorruptFileAndBackup_ResetToEmptyProgress()
        {
            File.WriteAllText(MainFile, "not json");
            File.WriteAllText(BackupFile, "");

            ProgressLoad load = Store().Load();

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.Reset));
            Assert.That(load.Progress.Levels, Is.Empty);
        }

        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"version\":0}")]
        [TestCase("[1, 2]")]
        public void AFileWithoutAValidVersion_IsUnreadable(string contents)
        {
            File.WriteAllText(MainFile, contents);

            Assert.That(Store().Load().Outcome, Is.EqualTo(LoadOutcome.Reset));
        }

        [Test]
        public void AnUnreadableFile_IsSetAsideForBugReports()
        {
            File.WriteAllText(MainFile, "not json");
            File.WriteAllText(BackupFile, V1(Record("level-01", 2)));

            Store().Load();

            Assert.That(File.ReadAllText(CorruptFile), Is.EqualTo("not json"));
        }

        [Test]
        public void ASetAsideFile_ReplacesAnOlderOne()
        {
            File.WriteAllText(CorruptFile, "older");
            File.WriteAllText(MainFile, "newer");

            Store().Load();

            Assert.That(File.ReadAllText(CorruptFile), Is.EqualTo("newer"));
        }

        [Test]
        public void AfterARecovery_TheNextSaveKeepsTheGoodBackup()
        {
            string backup = V1(Record("level-01", 2));
            File.WriteAllText(MainFile, "not json");
            File.WriteAllText(BackupFile, backup);
            var store = Store();
            store.Load();

            store.Save(Progress(("level-01", 3)));

            Assert.That(File.ReadAllText(BackupFile), Is.EqualTo(backup));
            Assert.That(Levels(new JsonProgressStore(directory).Load()), Is.EqualTo(new[] { ("level-01", 3) }));
        }

        [Test]
        public void AFileFromANewerVersion_IsNeverOverwritten()
        {
            // After a downgrade: run on defaults in memory and leave the newer file alone (ADR-0006).
            const string newer = "{\"version\":2,\"levels\":[],\"somethingNew\":true}";
            File.WriteAllText(MainFile, newer);
            var store = Store();

            ProgressLoad load = store.Load();
            bool saved = store.Save(Progress(("level-01", 3)));

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.NewerVersion));
            Assert.That(load.Progress.Levels, Is.Empty);
            Assert.That(saved, Is.False);
            Assert.That(File.ReadAllText(MainFile), Is.EqualTo(newer));
        }

        [Test]
        public void RecordsWithStarsOutsideOneToThree_AreDropped_AndTheRestKept()
        {
            File.WriteAllText(MainFile, V1(Record("level-01", 2) + "," + Record("level-02", 0) + "," + Record("level-03", 4)));

            ProgressLoad load = Store().Load();

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.Loaded));
            Assert.That(Levels(load), Is.EqualTo(new[] { ("level-01", 2) }));
            Assert.That(load.DroppedRecords, Is.EqualTo(2));
        }

        [Test]
        public void RecordsWithAnEmptyKey_AreDropped()
        {
            File.WriteAllText(MainFile, V1(Record("", 2) + "," + Record("level-01", 1)));

            ProgressLoad load = Store().Load();

            Assert.That(Levels(load), Is.EqualTo(new[] { ("level-01", 1) }));
            Assert.That(load.DroppedRecords, Is.EqualTo(1));
        }

        [Test]
        public void ARepeatedKey_KeepsTheHigherStars()
        {
            File.WriteAllText(MainFile, V1(Record("level-01", 1) + "," + Record("level-01", 3) + "," + Record("level-01", 2)));

            ProgressLoad load = Store().Load();

            Assert.That(Levels(load), Is.EqualTo(new[] { ("level-01", 3) }));
            Assert.That(load.DroppedRecords, Is.EqualTo(2));
        }

        [Test]
        public void RecordsForUnknownLevels_AreKept()
        {
            // The store knows no catalog; a level removed in an update keeps its record (ADR-0006).
            Store().Save(Progress(("level-99", 3)));

            Assert.That(Levels(Store().Load()), Is.EqualTo(new[] { ("level-99", 3) }));
        }

        [Test]
        public void VolumesOutsideZeroToOne_AreClamped()
        {
            File.WriteAllText(MainFile, V1("", "{\"sfxVolume\":-1,\"musicVolume\":7,\"haptics\":true}"));

            GameSettings settings = Store().Load().Progress.Settings;

            Assert.That(settings.SfxVolume, Is.EqualTo(0f));
            Assert.That(settings.MusicVolume, Is.EqualTo(1f));
        }

        [Test]
        public void Save_ReturnsFalseInsteadOfThrowing_WhenItCannotWrite()
        {
            string blocked = Path.Combine(directory, "not-a-folder");
            File.WriteAllText(blocked, "a file where the save folder should be");

            Assert.That(new JsonProgressStore(blocked).Save(Progress(("level-01", 1))), Is.False);
        }

        // Golden files: one sample per shipped version, loaded forever (ADR-0006).
        [Test]
        public void TheVersion1GoldenFile_Loads()
        {
            File.Copy(Path.Combine(GoldenFolder, "progress-v1.json"), MainFile);

            ProgressLoad load = Store().Load();

            Assert.That(load.Outcome, Is.EqualTo(LoadOutcome.Loaded));
            Assert.That(Levels(load), Is.EqualTo(new[] { ("level-01", 3), ("level-02", 1) }));
            Assert.That(load.Progress.Settings, Is.EqualTo(new GameSettings(sfxVolume: 0.8f, musicVolume: 0.6f, haptics: true)));
            Assert.That(load.Progress.IntroSeen, Is.True);
            Assert.That(load.DroppedRecords, Is.Zero);
        }
    }
}
