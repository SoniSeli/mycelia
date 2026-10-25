using System;
using System.Collections.Generic;
using Mycelia.Domain.Levels;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The player's progress for this run of the app: loaded once at boot, changed in memory, and written only at
    /// the save moments of ADR-0006 through <see cref="SaveIfChanged"/>. A failed write keeps the changes unsaved,
    /// so the next save moment tries again; the game carries on from memory.
    /// </summary>
    public sealed class PlayerProgress
    {
        readonly IProgressStore store;
        GameSettings settings;
        readonly bool introSeen;

        PlayerProgress(IProgressStore store, Progression levels, GameSettings settings, bool introSeen, LoadOutcome loadOutcome)
        {
            this.store = store;
            Levels = levels;
            this.settings = settings;
            this.introSeen = introSeen;
            LoadOutcome = loadOutcome;
        }

        public static PlayerProgress Load(IProgressStore store, IReadOnlyList<string> levelOrder)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (levelOrder == null) throw new ArgumentNullException(nameof(levelOrder));

            ProgressLoad load = store.Load();
            SavedProgress saved = load.Progress;
            return new PlayerProgress(store, new Progression(levelOrder, saved.Levels), saved.Settings, saved.IntroSeen, load.Outcome);
        }

        /// <summary>Unlocks and best stars per level (design §3.6).</summary>
        public Progression Levels { get; }

        /// <summary>How the save was found at boot, for the <c>save_recovered</c> / <c>save_reset</c> events (slice 6).</summary>
        public LoadOutcome LoadOutcome { get; }

        public bool HasUnsavedChanges { get; private set; }

        /// <summary>The player's settings, as last loaded or changed.</summary>
        public GameSettings Settings => settings;

        /// <summary>Changes the settings in memory. Returns true if anything changed; saving stays a separate step.</summary>
        public bool ChangeSettings(GameSettings value)
        {
            if (value.Equals(settings))
                return false;

            settings = value;
            HasUnsavedChanges = true;
            return true;
        }

        /// <summary>
        /// Records a win in memory. Returns true if it changed anything. Wins on a level outside the catalog (the
        /// Game scene's Debug Level) or a locked one (only reachable from the Editor) are not recorded.
        /// </summary>
        public bool RecordWin(string levelKey, int stars)
        {
            if (!Levels.IsUnlocked(levelKey))
            {
                Debug.LogWarning($"[Mycelia] The win on '{levelKey}' is not recorded: the level is not in the catalog or is locked.");
                return false;
            }

            if (!Levels.RecordWin(levelKey, stars))
                return false;

            HasUnsavedChanges = true;
            return true;
        }

        /// <summary>Writes the progress if anything changed since the last save. Returns false if the write failed.</summary>
        public bool SaveIfChanged()
        {
            if (!HasUnsavedChanges)
                return true;

            if (!store.Save(new SavedProgress(Levels.Records, settings, introSeen)))
                return false;

            HasUnsavedChanges = false;
            return true;
        }
    }
}
