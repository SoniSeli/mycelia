using System.Linq;
using Mycelia.Domain.Levels;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class PlayerProgressTests
    {
        static readonly string[] Order = { "level-01", "level-02", "level-03" };

        FakeProgressStore store;

        [SetUp]
        public void CreateStore() => store = new FakeProgressStore();

        PlayerProgress Load() => PlayerProgress.Load(store, Order);

        static SavedProgress Saved(params (string Key, int Stars)[] levels) =>
            new SavedProgress(levels.Select(l => new LevelRecord(l.Key, l.Stars)).ToArray(), GameSettings.Default, introSeen: false);

        [Test]
        public void WithNoSave_OnlyTheFirstLevelIsOpen()
        {
            PlayerProgress progress = Load();

            Assert.That(progress.LoadOutcome, Is.EqualTo(LoadOutcome.NoSave));
            Assert.That(progress.Levels.IsUnlocked("level-01"), Is.True);
            Assert.That(progress.Levels.IsUnlocked("level-02"), Is.False);
        }

        [Test]
        public void SavedRecords_DecideUnlocksAndStars()
        {
            store.Stored = Saved(("level-01", 2));

            PlayerProgress progress = Load();

            Assert.That(progress.Levels.IsUnlocked("level-02"), Is.True);
            Assert.That(progress.Levels.BestStars("level-01"), Is.EqualTo(2));
            Assert.That(progress.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void AWin_IsUnsavedUntilSaved()
        {
            PlayerProgress progress = Load();

            Assert.That(progress.RecordWin("level-01", 2), Is.True);
            Assert.That(progress.HasUnsavedChanges, Is.True);
            Assert.That(store.Saves, Is.Zero, "Recording a win never writes by itself.");

            Assert.That(progress.SaveIfChanged(), Is.True);
            Assert.That(progress.HasUnsavedChanges, Is.False);
            Assert.That(store.Stored.Levels.Single().BestStars, Is.EqualTo(2));
        }

        [Test]
        public void AWinThatBeatsNoRecord_LeavesNothingToSave()
        {
            store.Stored = Saved(("level-01", 3));
            PlayerProgress progress = Load();

            Assert.That(progress.RecordWin("level-01", 2), Is.False);
            Assert.That(progress.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void SaveIfChanged_WithNothingChanged_DoesNotWrite()
        {
            PlayerProgress progress = Load();

            Assert.That(progress.SaveIfChanged(), Is.True);
            Assert.That(store.Saves, Is.Zero);
        }

        [Test]
        public void AFailedSave_IsRetriedAtTheNextSave()
        {
            PlayerProgress progress = Load();
            progress.RecordWin("level-01", 1);
            store.Fails = true;

            Assert.That(progress.SaveIfChanged(), Is.False);
            Assert.That(progress.HasUnsavedChanges, Is.True, "Still unsaved after the failure.");

            store.Fails = false;
            Assert.That(progress.SaveIfChanged(), Is.True);
            Assert.That(store.Saves, Is.EqualTo(2));
            Assert.That(store.Stored.Levels.Single().Key, Is.EqualTo("level-01"));
        }

        [Test]
        public void ChangedSettings_AreUnsavedUntilSaved_AndSaveWithTheLevels()
        {
            store.Stored = new SavedProgress(new[] { new LevelRecord("level-01", 2) }, GameSettings.Default, introSeen: true);
            PlayerProgress progress = Load();
            var quiet = new GameSettings(sfxVolume: 0f, musicVolume: 0f, haptics: true);

            Assert.That(progress.Settings, Is.EqualTo(GameSettings.Default));
            Assert.That(progress.ChangeSettings(GameSettings.Default), Is.False, "Nothing changed.");
            Assert.That(progress.ChangeSettings(quiet), Is.True);
            Assert.That(progress.HasUnsavedChanges, Is.True);

            progress.SaveIfChanged();

            Assert.That(store.Stored.Settings, Is.EqualTo(quiet));
            Assert.That(store.Stored.Levels.Single().Key, Is.EqualTo("level-01"));
        }

        [Test]
        public void Saving_KeepsSettingsTheIntroFlagAndUnknownLevels()
        {
            var settings = new GameSettings(sfxVolume: 0.1f, musicVolume: 0.2f, haptics: false);
            store.Stored = new SavedProgress(new[] { new LevelRecord("level-99", 2) }, settings, introSeen: true);
            PlayerProgress progress = Load();

            progress.RecordWin("level-01", 3);
            progress.SaveIfChanged();

            Assert.That(store.Stored.Settings, Is.EqualTo(settings));
            Assert.That(store.Stored.IntroSeen, Is.True);
            Assert.That(store.Stored.Levels.Select(r => r.Key), Is.EquivalentTo(new[] { "level-99", "level-01" }));
        }

        [Test]
        public void AWinOnALevelOutsideTheCatalog_IsNotRecorded()
        {
            // level-test, the Game scene's Debug Level, is not in the catalog.
            PlayerProgress progress = Load();

            Assert.That(progress.RecordWin("level-test", 3), Is.False);
            Assert.That(progress.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void AWinOnALockedLevel_IsNotRecorded()
        {
            // Only possible from the Editor, by setting a locked level as the Debug Level.
            PlayerProgress progress = Load();

            Assert.That(progress.RecordWin("level-03", 3), Is.False);
            Assert.That(progress.Levels.IsUnlocked("level-03"), Is.False);
        }
    }
}
