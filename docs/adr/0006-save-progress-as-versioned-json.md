# ADR-0006: Save progress as a versioned JSON file; no mid-level resume

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-21 |
| **Deciders** | Soni Seli |
| **Amends** | [ADR-0005](0005-identify-spores-by-asset-with-runtime-id.md): drops the mid-level resume requirement it assumed |

## Context

Mycelia needs to persist a small amount of long-lived data on the device:

| Data | Example |
|---|---|
| Completed levels and best stars | `level-07`: ★★ |
| Settings | SFX volume, music volume, haptics on/off |
| One-time flags | Intro tutorial seen |

That's well under 2 KB. It must survive app updates, which may add, reorder or retune levels, and it must never be lost to a crash or the OS killing the app while it's being written.

[ADR-0005](0005-identify-spores-by-asset-with-runtime-id.md) assumed a **level in progress** would also be saved, so a killed app could resume mid-level. That needed a board snapshot, random number generator state, unknown-key handling and a second save file. Whether to build it was reconsidered here.

The prototype stored a single integer (highest unlocked level) in `PlayerPrefs`, with no versioning.

## Options considered

### Storage format
- **A. JSON files serialized with `JsonUtility`.** Built in, fast, low-allocation, and readable when debugging. Old versions can be kept as test fixtures. *Limit:* no dictionaries or polymorphism. These DTOs don't need either; lists convert to dictionaries in memory.
- **B. JSON with Newtonsoft (`com.unity.nuget.newtonsoft-json`).** Handles dictionaries and polymorphism, but is slower, allocates more, and offers flexibility these DTOs don't need.
- **C. `PlayerPrefs`.** Fine for a few values, but on Android it's an XML preferences file. It has no atomic multi-value write, no natural version number, and it's awkward to test.
- **D. Binary.** Smallest files, but unreadable, harder to migrate and review, and the size gain is meaningless at about 1 KB.

### Mid-level resume
- **Resume:** save the board, moves, objectives and RNG state after every move; restore on launch; handle unknown keys and changed levels.
- **No resume:** a level that's quit, killed or crashed simply restarts. Only progress is saved.

## Decision

We will store progress in **one versioned JSON file, `progress.json`, serialized with `JsonUtility`**, and we will **not** save a level in progress.

**Why not resume:** levels last 2–5 minutes, so the most a player can lose is one short level. Resume would add a board snapshot format, random-state serialization, unknown-key and changed-level handling, a second file and its migrations: a large, bug-prone surface for a small benefit in a 12-level v1. That scope is better spent on the core loop and polish.

**Why JsonUtility:** it is the cheapest option that meets every requirement (versioned, atomic, testable, readable). Its limits don't affect data this simple.

### File and data shape

`Application.persistentDataPath/progress.json`:

```json
{
  "version": 1,
  "levels": [
    { "key": "level-01", "bestStars": 3 },
    { "key": "level-02", "bestStars": 1 }
  ],
  "settings": { "sfxVolume": 0.8, "musicVolume": 0.6, "haptics": true },
  "introSeen": true
}
```

- **Levels are identified by a stable `LevelDefinition` key** (`"level-07"`), never by index. Inserting or reordering levels in an update can't move a player's stars onto the wrong level. This is the same reasoning as spore keys in ADR-0005.
- **There is no "highest unlocked" field.** Unlocks are *derived*: level `k + 1` is unlocked if level `k` has a record. That keeps a single source of truth, so the data can't contradict itself.
- **Unknown level keys are kept, not deleted** (for example, a level removed in an update). They're ignored for display and preserved on write, so a later update could restore them.

```csharp
[Serializable] sealed class ProgressDataV1     // one DTO class per shipped version, kept forever
{
    public int version = 1;
    public List<LevelRecord> levels = new();
    public SettingsData settings = new();
    public bool introSeen;
}
```

The unlock rule and the best-stars-only-goes-up rule are pure C# in the domain (`Progression`) and unit-tested. The App layer only maps between DTOs and the domain.

### Versioning and migration

1. Read the file and parse only `{ "version": n }` with a tiny probe DTO.
2. Deserialize into `ProgressDataV{n}`, then apply migrations one version at a time (`V1 → V2 → …`) up to the current one.
3. **Golden-file tests:** a sample `progress.json` for **every shipped version** is kept in the test folder, and a test loads each one and checks the migrated result.
4. A file from a **newer** version (after a downgrade) is not overwritten. The game runs with defaults in memory and leaves the file alone.

### Writing safely

- **Atomic write:** serialize, write `progress.json.tmp`, flush, then replace `progress.json` and keep the previous file as `progress.json.bak` (`File.Replace`).
- **Load order:** `progress.json` → `progress.json.bak` → defaults. A recovery or reset is logged as an analytics event (`save_recovered`, `save_reset`) and never crashes the game.
- **When it saves:**
  - **When a level is won.** Stars and the unlock are saved the moment the outcome is decided, **before** the celebration and fact card, so killing the app during the animation can't lose the unlock.
  - When the settings screen closes, if anything changed.
  - On `OnApplicationPause(true)`, if anything changed since the last save. This write is synchronous, because the OS may suspend the app immediately afterwards.
- Writes happen on the main thread. At under 2 KB and only at these non-gameplay moments, they shouldn't show up in the Profiler. This is to be checked on the reference device, and the write moves to a background thread if it ever does show up.

### Explicitly out of scope

- **Encryption or anti-tamper:** single-player, no economy, nothing worth protecting.
- **Cloud save:** a v1 non-goal. Saving goes through an `IProgressStore` interface, so a cloud implementation (e.g. Play Games Saved Games) could be added later without touching callers.

### Refinements (2026-10-24, Milestone 3 slice 1)

Decided while building the level catalog, which supplies the level order `Progression` derives unlocks from:

- **`LevelCatalog`** (Content) lists the levels players see, in play order, and finds them by key. `AppServices` carries it, and `AppRoot` opens its first level until Level Select exists.
- **Catalog keys must read `level-NN`** (`level-` and two digits). *Why:* keys look the same everywhere (saves, analytics and the examples above). The number is only a name, so reordering levels later never renames a key, and `level-07` need not stay seventh.
- **A guard test lists the shipped level keys** and fails if one disappears from the catalog; adding levels is fine. *Why:* a renamed or removed key silently drops players' stars and unlocks, and a test catches that in CI, where an Inspector tooltip can be missed. The list starts with the three placeholder keys, which the real levels keep in slice 7.
- **`level-test` stays outside the catalog**, as the Game scene's Debug Level and a test fixture (its key can't pass the `level-NN` rule, and a test checks it too). *Why:* the debug level and tests need a fixed level that changes to real levels can't break, and players should never see it.
- **Three placeholder levels for now**; the real 12 come in slice 7. *Why:* three is the smallest number that tests unlocking a chain of levels, the save file and level select, without designing levels early.

### Refinements (2026-10-24, Milestone 3 slice 2)

Decided while building the store (`JsonProgressStore` behind `IProgressStore`, in `Mycelia.App`):

- **Version 1 has the full shape above** (levels, settings, `introSeen`), though settings and the intro arrive in Milestone 4. *Why:* the first file players get already has every field, so Milestone 4 only starts using them, with no new version or golden file.
- **Bad records are dropped one by one**: stars outside 1–3, an empty key, or a repeated key (the highest stars are kept). The rest of the file still loads, and the count is reported. *Why:* one bad record should never cost the player the rest of their progress.
- **An unreadable `progress.json` is moved to `progress.json.corrupt`** (replacing an older one) before the backup is read. *Why:* a bug report can include the file that failed. It also keeps the next save from turning the bad file into the backup and losing the good one.
- **CI keeps per-run debug keys until the Play release.** *Why:* the phone runs local development builds, whose local debug key stays the same, so saves survive there; one keystore made for the release covers both.
- Smaller choices: default settings are SFX 0.8, music 0.6, haptics on; volumes read from a file are clamped to 0–1; `Load` reports an outcome (`NoSave`, `Loaded`, `RecoveredFromBackup`, `Reset`, `NewerVersion`) for the `save_recovered` / `save_reset` events in slice 6; neither call throws, and a failed save returns false and logs a warning. Once a newer-version file is seen, the store refuses to save for the rest of the session.

### Refinements (2026-10-24, Milestone 3 slice 3)

Decided while wiring saves into the game (`PlayerProgress` on `AppServices`, `LevelResultRecorder` between chain input, playback and the HUD):

- **A win is recorded in memory when the winning move is committed, and written when that move has finished playing**, just before the result panel shows. `AppRoot` also saves on `OnApplicationPause(true)` if anything is unsaved. *Why:* no disk write lands on a move frame (the 25 ms budget), and the win is in memory from the moment it is decided, so going to the background during the winning animation still saves it. Only a crash during that second loses it. The write allocates once per level, at its end; the 0 B per move rule (ADR-0009) is unaffected.
- **A failed save is retried quietly**: a warning is logged, the changes stay marked unsaved, and the next save moment tries again. *Why:* an error that should almost never happen isn't worth UI and text; the game keeps working from memory.
- **Checked on the phone in this slice** (Redmi Note 12 Pro+, development build). *Why:* it catches an Android-only problem with the file swap before level select and the flow build on it. Result: the save lives in `/sdcard/Android/data/com.soniseli.mycelia/files`; with an existing file, a win replaced it through `File.Replace` (the old file became `.bak`, no `.tmp` left), and both records loaded again after a force-stop.
- Smaller choices: wins on a level outside the catalog (the Debug Level) or a locked one are not recorded and log a warning. Pressing Play in the Editor saves to the Editor's own `persistentDataPath`. Development builds log the load outcome at boot. JsonUtility writes floats at full precision (`0.800000011920929`); they read back as the same float.

### Refinements (2026-10-25, Milestone 4 slice 5)

- **Settings are saved the moment they change.** The settings modal reports every switch it flips; `PlayerProgress.ChangeSettings` records the new settings in memory and the save follows at once, so nothing waits for the next save moment. Flipping a switch back and forth writes once per flip; choosing what is already saved writes nothing.
- **The save format doesn't change.** The modal has on/off switches; the save keeps v1's volumes. A sound is on while its volume is above 0; switching it off saves 0, switching it on keeps a volume it still has or brings back the default (0.8 for effects, 0.6 for music). Volume sliders, if they ever come, need no migration.

## Consequences

- **Positive:**
  - A small, well-tested persistence layer: one file, one DTO per version, golden-file migration tests, atomic writes with a backup.
  - Stable level keys and derived unlocks make saves robust to content updates.
  - Dropping resume removes the riskiest persistence code from v1. Spore keys (ADR-0005) are no longer persisted.
- **Negative:**
  - If the OS kills the app in the background, or it crashes, **the current level restarts** with full moves. For 2–5 minute levels this is an accepted trade-off.
  - The lifecycle story for reviewers becomes "progress is never lost, and the game pauses correctly" rather than "you resume exactly where you were."
- **Follow-up:**
  - `OnApplicationPause(true)` during a level shows a pause overlay, and the level waits for the player to come back. The game never continues to run in the background. *Done in Milestone 2 slice 7 ([ADR-0013](0013-pause-the-level-and-handle-back.md)); losing focus pauses too. Saving on pause comes with the save system.*
  - Analytics: log `level_start` and `level_end`. A `level_start` with no matching end, followed by a later session, suggests a level lost to backgrounding.  *Done in Milestone 3 slice 6 ([ADR-0014](0014-log-playtest-analytics-to-a-local-file.md)); a level the player leaves ends as `abandoned`, so a missing end means the app was killed.*
  - **Revisit if:** that rate is high, or levels become longer (more than about 5 minutes). Resume would then become a new ADR, building on ADR-0005's key-based board format.
