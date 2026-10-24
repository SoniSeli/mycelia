namespace Mycelia.App
{
    public enum LoadOutcome
    {
        /// <summary>No save yet (first launch).</summary>
        NoSave,
        Loaded,
        /// <summary>The main file was missing or unreadable; the backup was used (analytics: <c>save_recovered</c>).</summary>
        RecoveredFromBackup,
        /// <summary>Save files existed but none could be read; progress starts over (analytics: <c>save_reset</c>).</summary>
        Reset,
        /// <summary>The file is from a newer version of the game; it runs on defaults and never overwrites it.</summary>
        NewerVersion,
    }

    public readonly struct ProgressLoad
    {
        public readonly SavedProgress Progress;
        public readonly LoadOutcome Outcome;

        /// <summary>Records dropped because they were invalid (bad stars, empty key, or a repeated key).</summary>
        public readonly int DroppedRecords;

        public ProgressLoad(SavedProgress progress, LoadOutcome outcome, int droppedRecords)
        {
            Progress = progress;
            Outcome = outcome;
            DroppedRecords = droppedRecords;
        }
    }

    /// <summary>Loads and saves the player's progress (ADR-0006). Neither call throws: saving must never crash the game.</summary>
    public interface IProgressStore
    {
        ProgressLoad Load();

        /// <summary>True if the progress is safely on disk.</summary>
        bool Save(SavedProgress progress);
    }
}
