# Mycelia: Design Document

| | |
|---|---|
| **Status** | Draft |
| **Author** | Soni Seli |
| **Last updated** | 2026-10-25 |
| **Related** | [Performance budget](performance-budget.md) · [Decision records](adr/README.md) |

> **How to read this draft.** Sections marked **Decide** are open questions to settle (most become an ADR). Everything else is a proposal: rewrite it in your own words before the first commit, since this document is part of your portfolio.

---

## 1. Overview

**Pitch.** Mycelia is a portrait, one-thumb puzzle game for mobile. Players connect neighbouring spores of the same kind to clear them from the board and complete each level's collection goals within a limited number of moves. Finishing a level unlocks a short, illustrated fact about fungi and mycelium networks.

**Platforms.** Android first (minimum API level _TBD_), iOS second.

**Audience.** Casual players who play short sessions (2–5 minutes per level).

> **Decide:** One sentence on why you built this. A recruiter will ask; the educational angle (learning about fungi through play) is a strong answer.

## 2. Goals and non-goals

**Goals**
- A complete, polished 12-level experience that feels good to play on a low-end Android phone.
- A codebase that shows production habits: game logic you can test, measured performance, decisions you can explain.
- Ship it publicly (Play Store internal or open testing, or itch.io).

**Non-goals (for v1)**
- Monetisation (ads, in-app purchases).
- Online features (accounts, leaderboards, cloud save).
- Procedurally generated levels.
- A level editor (possible v1.1 stretch goal).

## 3. Game rules

_Write these in your own words from how the game plays. They become the spec for the domain tests in Milestone 1._

### 3.1 Board
- A rectangular grid of cells, **6 × 6** by default; a level may override the size.
- Every cell holds exactly one **spore** of a given **spore type**. Each level declares the spore types in play (default: 4).
- Each spore type is a **fungus species** with its own colour *and* silhouette, so types never rely on colour alone ([ADR-0005](adr/0005-identify-spores-by-asset-with-runtime-id.md)). The silhouettes are circles with different outlines: smooth, scalloped, spiked and notched (Milestone 4).
- Cells are addressed by `(column, row)`, with `(0, 0)` at the bottom-left.

### 3.2 Making a chain
- The player presses on a spore and drags through neighbouring spores of the **same type**.
- **Neighbours are 8-way:** two cells are neighbours if they differ by at most 1 in both column and row (Chebyshev distance 1), so diagonals count.
- Dragging back onto the previous spore removes the last link (undo while dragging).
- A spore can only appear once in a chain.
- Releasing with a chain of **at least 2** spores commits it as a move. A single spore is cancelled and costs no move.

> **Why 8-way and a minimum of 2:** This keeps the game easy to pick up for short, one-thumb sessions, and it matches the prototype, where all 12 levels were playtested and balanced. Pairs are allowed but low value (see 3.4), so players are still nudged toward longer chains. The cost is that diagonal drags pass close to the side neighbours, so touch targets must be designed for it (see §6, Input).

### 3.3 Resolving a move
1. Chained spores are removed.
2. Spores above the gaps fall straight down to fill them (gravity per column, preserving order). Void cells are skipped: spores fall past them but never into them.
3. Empty cells at the top of each column are refilled. Each new spore's type is chosen **uniformly at random from the level's spore types**.
4. The move counter decreases by 1.
5. **Reshuffle guarantee:** if the board now has no valid chain (no two neighbouring spores of the same type), the existing spores are shuffled (type counts are kept) until at least one valid chain exists. A reshuffle costs no move. If a set number of shuffle attempts all fail, the board is regenerated instead.

> **Why uniform plus reshuffle:** Uniform odds are fair and easy to explain, and only spawning the level's own types fixes a prototype bug where a level could ask for types it never spawned. With 8-way neighbours, a minimum of 2 and 4 types on a 6 × 6 board, a dead board is extremely unlikely. The guarantee is still cheap, it removes a soft-lock entirely, and it makes a clear test case. Difficulty is tuned through moves and objective targets, not hidden spawn odds.

### 3.4 Stars (no score)
- There is **no score**. Completing the objectives is the only goal.
- On a win, the player earns **1–3 stars** based on the fraction of the level's moves still left:

  | Stars | Moves remaining |
  |---|---|
  | ★ | Level won |
  | ★★ | ≥ 25 % of starting moves |
  | ★★★ | ≥ 50 % of starting moves |

- The thresholds are global defaults, and a level may override them. The percentages are first guesses, to be tuned in playtesting.
- The best star result per level is saved; replaying can only raise it.

> **Why stars instead of score:** In the prototype, score was left over from the template's endless mode and had no effect in a level-based game. An unused system is something reviewers notice. Moves left already measures how well you played, and stars by moves left give players a reason to replay (a common mobile retention mechanic) with a single setting per level. Rewarding efficient play also rewards long chains, so nothing is lost.

### 3.5 Objectives, winning and losing
- Each level defines one or more objectives: _collect X spores of type T_.
- Clearing spores of type T counts toward that objective, capped at its target.
- Every objective's spore type must be one of the level's spore types. This is checked when the level is authored and covered by a test.
- **Win:** all objectives are complete. The game checks this after each move resolves.
- **Lose:** no moves remain and at least one objective is incomplete.

### 3.6 Levels and progression
- 12 hand-authored levels, unlocked in order.
- A level definition contains: index, board size, number of moves, spore types in play, objectives, optional star-threshold overrides, and the fact card (text, image, image credit).
- Completing level `k` unlocks level `k + 1`. Unlocked levels and best stars per level persist between sessions.
- Progress is stored in a versioned `progress.json`, with levels identified by stable keys (`"level-07"`) ([ADR-0006](adr/0006-save-progress-as-versioned-json.md)).
- **A level in progress is not saved.** If the player quits, or the app is killed or crashes mid-level, that level restarts with full moves and no penalty. Levels are 2–5 minutes long, so this is an accepted trade-off. Stars and unlocks are saved the moment a level is won, before the celebration plays.

## 4. Player flow

```
Launch → Main menu ⇄ Settings
              │
              ├── (first Play, until finished) Intro ──┐
              ▼                                        ▼
         Level select ── help (?) ──▶ Intro ──▶ Level select
                                         │
                                         ▼
                                       Level ── win ──▶ Stars + fact card ──▶ Level select
                                         │
                                         └── lose ──▶ Retry / Level select
```

> **Decided 2026-10-24:** launch opens a main menu (title and Play) before level select, as the game's front door for a portfolio piece people open once to judge ([ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) Refinements). Back steps out one screen at a time, and leaves the app from the main menu. *Decided 2026-10-26 (Milestone 4 slice 8):* the intro opens after the first tap on the mushroom rather than before the main menu, and ends in level select ([ADR-0010](adr/0010-hand-written-composition-root.md) Refinements).

## 5. Architecture (proposal)

Three layers, each depending only on the one below it:

| Layer | Contains | Unity dependency | Tested by |
|---|---|---|---|
| **Domain** | Board, chain rules, gravity/refill, reshuffle, objectives, star rating, level session state machine | None (pure C#) | EditMode unit tests |
| **Game** | Board view, spore views (pooled), chain input, HUD, panels | MonoBehaviours | PlayMode smoke tests and manual testing |
| **App** | Bootstrap, scene loading, save, audio, haptics, settings, analytics | MonoBehaviours and services | Manual testing on device |

**Data.** `SporeType` (stable key, species name, colour, unique silhouette sprite, accessible label), `SporeCatalog`, `LevelDefinition` and `LevelCatalog` are ScriptableObjects. The domain refers to spore types only by a 1-byte runtime `SporeTypeId`, and analytics uses the stable key. Neither ever uses colour ([ADR-0005](adr/0005-identify-spores-by-asset-with-runtime-id.md)). These ScriptableObjects and `LevelDefinitionMapper` (asset → `LevelConfig`) live in a fourth assembly, **`Mycelia.Content`**, between Domain and Game, because both the Game views (sprites, colours) and the App (level select, saves) need them. Its Editor assembly shows each asset's problems in the Inspector, and EditMode tests check that every level asset in the project is playable and that the one `LevelCatalog` is valid and still holds every shipped level key ([ADR-0006](adr/0006-save-progress-as-versioned-json.md) Refinements).

**Communication.** The domain is an engine-free assembly. Committing a chain runs a fixed pipeline of phases (Clear → Trigger → Damage → Gravity → Refill → Hazards → Settle → Evaluate). Each phase's rules record typed steps, and the resulting `MoveResult` is played back by the Game layer phase by phase, with input locked ([ADR-0003](adr/0003-separate-pure-domain-from-views.md), [ADR-0004](adr/0004-resolve-moves-through-a-phase-pipeline.md)). There are no global static event buses.

**Extending the game.** A new mechanic is added as new rule, step and step-player classes, plus one line registering it in the pipeline. Cells are only accessed through the `Board` API, so new cell layers (obstacles, hazards) don't change existing callers. Objectives implement `IObjective`. See the mold walkthrough in ADR-0004.

**Dependencies.** A single bootstrap scene creates the services and passes them to the scenes that need them. There are no `Resources`-loaded singletons and no static state carried between scenes.

**Rendering.** URP with the 2D Renderer; sprites are unlit unless a measured effect needs lighting ([ADR-0002](adr/0002-use-urp-with-2d-renderer.md)).

**Persistence.** One versioned `progress.json` serialized with `JsonUtility`, with golden-file migration tests and atomic writes. Invalid records are dropped one by one, and an unreadable file is set aside as `progress.json.corrupt` before falling back to the backup. `AppRoot` loads it once into `PlayerProgress`; a win is recorded when its move is committed and written when that move finishes, and the app saves when it goes to the background. There is no mid-level resume ([ADR-0006](adr/0006-save-progress-as-versioned-json.md)).

**Input.** One Input System action bound to `<Pointer>` (primary touch on device, mouse in the Editor); screen positions map to cells with grid maths and the circular hit radius from §6 ([ADR-0008](adr/0008-pointer-action-and-grid-hit-testing.md)).

**Animation.** `MovePlayback` turns each `MoveResult` into one PrimeTween Sequence (steps grouped within a phase, phases chained), installed only through npm per its licence and written to stay allocation-free ([ADR-0009](adr/0009-animate-playback-with-primetween.md)).

**Wiring.** `AppRoot` in the Bootstrap scene creates the services once and passes them to each scene's single entry point; no singletons or static state ([ADR-0010](adr/0010-hand-written-composition-root.md)). Every scene change fades through the backdrop's gradient, and nothing can be pressed while the screen changes.

**Analytics.** Events go through an `IAnalytics` service to a local `events.jsonl` on the device, echoed to logcat in development builds: launch and save health, and each level attempt's start and end (won, lost or abandoned). Nothing leaves the phone ([ADR-0014](adr/0014-log-playtest-analytics-to-a-local-file.md)).

**Board view.** The board stays in world units (one cell = one unit) and the camera is fitted to it inside the safe area, leaving tunable bands for the HUD. Spore views come from a pool filled before play, and board sprites share one atlas ([ADR-0011](adr/0011-fit-camera-to-board-and-pool-views-up-front.md)).

**UI.** uGUI with TextMeshPro, with canvases split by how often they change, safe-area fitting, and allocation-free text updates ([ADR-0012](adr/0012-use-ugui-with-textmeshpro.md)). The main menu and level select are scenes of their own, each with an entry; level select shows every level on one 3 × 4 grid.

**Lifecycle.** `LevelPause` pauses a level when the app goes to the background or loses focus, or on the back button: input locks, a chain in progress is cancelled, the playing move freezes, and an overlay offers Resume, Restart and Quit. The HUD's pause button opens it too. Back on the result panel sends the app to the background ([ADR-0013](adr/0013-pause-the-level-and-handle-back.md)).

## 6. Mobile considerations

- **Input:** touch through the Input System, with mouse as a fallback for the Editor. A second finger is ignored while dragging. Because chains can go diagonally (§3.2), a spore joins the chain only when the finger comes within a **circular** radius of its centre (0.45 of a cell, tuned on the mid-range phone; still to check on the low-end device), not anywhere in its square cell. That way a diagonal drag doesn't accidentally catch the side neighbours it passes between.
- **Screen:** portrait only. Supports aspect ratios from 16:9 to about 21:9 and tablets, with layout inside the device's safe area.
- **App lifecycle:** progress saves atomically (temporary file → replace, with a backup) when a level is won, when settings change, and when the app is paused. A level in progress is not resumed (§3.6). When the app is paused or loses focus during a level, it shows a pause overlay and waits for the player. Audio pauses on interruptions. The Android back button behaves sensibly on every screen ([ADR-0013](adr/0013-pause-the-level-and-handle-back.md)).
- **Performance:** see [performance-budget.md](performance-budget.md). The target is a low-end reference device, not the Editor.
- **Accessibility:** colour-blind support is **always on, not a setting**. Every spore type is a species with a unique silhouette (a circle with its own outline), enforced by a test that compares their pixels, and is checked in grayscale and in colour-blindness simulations. Text is readable at phone size. Haptics can be turned off.
- **Localisation:** _out of scope for v1?_ If so, keep fact text in data so it can be localised later.

## 7. Testing strategy

- **Domain:** EditMode tests written alongside the code. Test names read as rules, e.g. `Chain_RejectsDifferentSporeType`, `Gravity_LeavesNoGapsBelowSpores`. Boards are written as text grids (`Boards.Parse`), so a test reads like the rule it checks. One test asserts the pipeline's phase order.
- **Randomness:** the domain takes an injected `IRandom` (PCG32) seeded explicitly in every test; in the game each attempt gets a fresh, logged seed, so any board can be replayed exactly ([ADR-0007](adr/0007-random-seed-per-attempt-with-pcg32.md)).
- **Game/App:** a small number of PlayMode smoke tests (a level loads and a scripted chain resolves), plus a manual device checklist before each release.
- **CI:** every push runs the EditMode tests and builds an Android APK. PlayMode tests (move playback, including the 0 B allocation test) run locally for now ([ADR-0009](adr/0009-animate-playback-with-primetween.md)). Each CI run signs its APK with a new debug key, so a CI build can't update an installed one: uninstall first. *Decided 2026-10-23 to keep it that way for now: there are no saves until Milestone 3, so uninstalling costs nothing yet. A project keystore in CI secrets comes with the save system or the Play release.* *Decided 2026-10-24 (Milestone 3 slice 2): with the Play release. Local development builds keep their debug key, so saves survive on the phone; installing a CI build still means an uninstall, which now also wipes progress.*
- **Device performance:** `PerfProbe` logs frame, main-thread, render-thread and GPU times, GC, batches and SetPass calls in development builds; 0 B per move with the real HUD is checked with it during real moves ([performance budget](performance-budget.md)).

## 8. Milestones

| # | Milestone | Done when |
|---|---|---|
| 0 | Repo, docs, CI building an empty Android project | CI is green and the APK installs on the reference device. *Amended 2026-10-22: installed on the mid-range device; the low-end reference device is deferred to Milestone 2.* |
| 1 | Domain logic and tests | Every rule in §3 is covered by a passing test |
| 2 | Board view, touch input, pooling, lifecycle, safe area | Low-end reference device chosen; one level is playable on it within budget. *Amended 2026-10-23: one level is playable on the mid-range phone within budget; the low-end reference device moves to Milestone 5 ([performance budget](performance-budget.md)). Amended 2026-10-24: "within budget" covers the lines measured in slice 8; the live Profiler connection and the rest move to Milestone 5. Done 2026-10-24.* |
| 3 | Level flow, save system, analytics events | All 12 levels are playable and progress persists. *Done 2026-10-24 on the mid-range phone (slice 8).* |
| 4 | App shell, back button, haptics, colour-blind mode | Full flow works on device. *Amended 2026-10-24: Milestone 4 carries Soni's UI/UX overhaul (new look on every screen, onboarding, settings, outline-differentiated spores) before haptics and audio; plan in §8.3.* |
| 5 | Content and build-size pass | Build size and memory are within budget, on the low-end reference device too (moved here from Milestone 2); managed stripping level decided. *Moved here from Milestone 2 on 2026-10-24: the live Profiler connection, and the budget lines not yet measured (memory growth over 10 levels, cold start to level select, the 15-minute session, overdraw, scripts-only CPU time).* |
| 6 | Release | Public build link, README, GIF, performance write-up |

### 8.1 Milestone 2 plan

Slices in order; each lands as its own commit(s) with tests. Pushes are batched.

| # | Slice | Builds on | Done when |
|---|---|---|---|
| 1 | **Spore and level data**: `SporeType`, `SporeCatalog`, `LevelDefinition` ScriptableObjects, mapping to `LevelConfig`, Editor validation; four placeholder silhouettes (final species art comes later) | ADR-0005 | A test level asset loads into a `LevelConfig`; invalid assets show their problems in the Inspector |
| 2 | **App bootstrap**: `AppRoot`, `AppServices`, `SceneLoader`, `ISceneEntry`, Editor play-mode start scene, frame-rate policy | ADR-0010 | Pressing Play in any scene boots through Bootstrap into the Game scene with a test level |
| 3 | **Board view**: `BoardLayout` fitted to the safe area (portrait, 16:9 to ~21:9, tablets), pooled `SporeView`s, sprite atlas | ADR-0002, ADR-0003 | The starting board renders correctly on several aspect ratios; no `Instantiate`/`Destroy` during play |
| 4 | **Touch input**: `MyceliaInput` actions, `CellHitTest` (radius, swipe sampling) with EditMode tests, `ChainInputController`, chain line | ADR-0008 | Chains can be dragged on device, including diagonals and backtracking, with no stray side cells |
| 5 | **Move playback**: PrimeTween via npm, `PlaybackSettings`, `MovePlayback`, input lock, PlayMode allocation test | ADR-0009 | A move animates pop → fall → refill (→ reshuffle); playback allocates 0 B after warm-up |
| 6 | **HUD**: moves left, objective counters, win/lose panel (minimal) | ADR-0012 | A level can be won or lost and the result is shown |
| 7 | **Lifecycle**: pause overlay on `OnApplicationPause`, Android back button, audio pause, 30/60 fps policy. *Audio pause deferred until there is audio (ADR-0013).* | ADR-0006, ADR-0010, ADR-0013 | Backgrounding mid-level pauses cleanly; back button behaves on every screen |
| 8 | **Device pass**: choose the low-end reference device; Profiler connection check (deferred from Milestone 0); memory investigation (the 246 MB case study); frame time, GC and batches against the budget. *Measured on the mid-range phone only: `PerfProbe`, batches replaced by render-thread time and SetPass, memory budget raised to 300 MB; the low-end device moved to Milestone 5. A Profiler capture recorded on the phone traced the small GC allocations during play: two per-level costs in our code (fixed, with tests, and re-measured: 0 B per frame during moves after warm-up) and first-use costs counted as warm-up. Long frames were waits on the display; the one 25.9 ms frame stays on the watch list.* | Performance budget | Results logged on both devices; Milestone 2 done-when met. *Amended 2026-10-24: results logged on the mid-range phone; the low-end device, the live Profiler connection and the budget lines not yet measured move to Milestone 5. Done.* |

### 8.2 Milestone 3 plan

Slices in order; each lands as its own commit(s) with tests. Pushes are batched. Already in place from earlier milestones: `Progression` (derived unlocks, best stars only go up, unknown keys kept; Milestone 1), the fact-card fields on `LevelDefinition`, and `GameSceneArgs` carrying the level into the Game scene.

| # | Slice | Builds on | Done when |
|---|---|---|---|
| 1 | **Level catalog**: `LevelCatalog` ScriptableObject (levels in play order) on `AppServices`, Inspector validation (unique keys, no empty entries, every level valid), replacing `AppRoot`'s single start level. *Decided 2026-10-24: three placeholder levels for now (the real 12 come in slice 7); keys must match `level-NN`; a guard test lists the shipped keys and fails if one disappears; `level-test` stays outside the catalog as the Game scene's Debug Level and test fixture.* | ADR-0005, ADR-0006, ADR-0010 | The game starts from the catalog; an EditMode test checks every catalog level is playable and every key is unique. *Done 2026-10-24.* |
| 2 | **Save system**: `IProgressStore`, `ProgressDataV1`, JSON store with atomic write (`.tmp` → `File.Replace`, `.bak`), load order file → backup → defaults, newer-version files left alone, `SavedProgress` (records ready for `Progression`, settings, intro flag), golden-file test for v1. *Decided 2026-10-24: v1 has the full ADR-0006 shape; bad records are dropped one by one; an unreadable file is set aside as `.corrupt`; the CI keystore waits for the Play release (ADR-0006 Refinements).* | ADR-0006 | Progress round-trips; a corrupt file falls back to the backup, then to defaults; a newer file is never overwritten. *Done 2026-10-24 (not yet wired into the app: slice 3).* |
| 3 | **Saving at the right moments**: the win is recorded and saved before the result panel and celebration; save on `OnApplicationPause(true)` if anything changed. *Decided 2026-10-24: record at commit, write when the winning move finishes; failed saves retried quietly; checked on the phone in this slice (ADR-0006 Refinements).* | ADR-0006, ADR-0013 | Killing the app during the win celebration keeps the unlock. *Done 2026-10-24: on the phone, a win replaced an existing save and survived a force-stop.* |
| 4 | **Main menu and level select**: a `MainMenu` scene (title, Play) and a `LevelSelect` scene (locked/unlocked levels, best stars); back steps out one screen; back on the result panel opens level select. *Decided 2026-10-24: main menu first; separate scenes; fixed 3 × 4 grid; locked levels show a lock and don't react; buttons show the number and best stars (ADR-0012 and ADR-0013 Refinements).* | ADR-0010, ADR-0012, ADR-0013 | Any unlocked level can be started from it; locked ones can't. *Done 2026-10-24 (in the Editor; the phone check comes with slice 8 unless asked sooner).* |
| 5 | **Level flow**: win → stars + fact card → level select; lose → retry / level select; Level select on the pause overlay. *Decided 2026-10-24: no Next level button (back to the grid to see progress); the fact card on the result panel, on every win; Retry and Level select after a loss; Level select added to the pause overlay (ADR-0012 Refinements).* | §4, ADR-0012 | The full loop works in the Editor without restarting Play. *Done 2026-10-24.* |
| 6 | **Analytics events**: an `IAnalytics` service; level start/end with key, seed and moves left, `save_recovered`, `save_reset`. *Decided 2026-10-24: a local `events.jsonl`, echoed to logcat in development builds; `app_start`, `save_recovered`/`save_reset`, `level_start`/`level_end`; leaving a level ends it as `abandoned`; no duration ([ADR-0014](adr/0014-log-playtest-analytics-to-a-local-file.md)).* | ADR-0006, ADR-0007, ADR-0014 | Events reach the chosen sink in tests and on device. *Done 2026-10-24 in tests and the Editor; on the phone in slice 8.* |
| 7 | **The 12 levels**: level assets and fact cards (credits in §10). *Decided 2026-10-24: a drafted curve checked by a simulated bot over 500 seeds per level ([ADR-0015](adr/0015-balance-levels-with-a-simulated-bot.md), results in [balancing.md](balancing.md)); rectangles only; Soni's facts on what mycelium can do, kept in full as placeholder text until the Milestone 5 content pass, on a card that grows with its text; Soni's photos local-only until licensed ([facts.md](facts.md)).* | §3.6, ADR-0015 | All 12 are in the catalog and pass validation. *Done 2026-10-24 (the photos only in local builds).* |
| 8 | **Device pass**: play through on the phone, kill the app mid-level and after a win, quick `PerfProbe` check of the new screens. *Done 2026-10-24: all 12 levels won on the phone; progress survived swiping the app away mid-level and a force-stop right after a win; events as expected. The bot replayed the same seeds ([balancing.md](balancing.md): curve and star thresholds kept). Moves on 7 × 7 boards reached 31–46 ms in development builds, traced to building PrimeTween sequences and 0.45–2.2 ms in release (judged on release timing, [performance budget](performance-budget.md); ADR-0009 Refinements). The photos now import compressed: release APK 38.7 MB, memory 264–266 MB ([facts.md](facts.md)).* | Performance budget | Milestone 3 done-when met on the mid-range phone. *Done.* |

> **Decide before starting:** placeholder or final level design for the 12 levels, and where fact images come from. *Decided along the way: the CI keystore with the Play release (slice 2, §7); save moments (slice 3); main menu first (slice 4, §9); analytics to a local file (slice 6, ADR-0014).*

### 8.3 Milestone 4 plan (draft)

Soni's UI/UX brief (2026-10-24) sets the look: a deep blue to turquoise vertical gradient and slowly drifting glowing spores behind every screen, one rounded font for all text, and soft, organic, round shapes with no hard grid boxes. Most of the brief overlaps Milestone 4's theme (settings, first-run intro, colour-blind spores, app shell), so it comes first, and haptics and audio follow. The mockups and the brief are kept outside the repository; Soni's own mushroom (cap and stem) and level-node shape are in `Art/UI`. Already in place: back on every screen (ADR-0013), the main menu and level select (Milestone 3), and the save's settings and `introSeen` fields.

| # | Slice | Builds on | Done when |
|---|---|---|---|
| 1 | **Design system**: the game font (an SIL Open Font License font close to the brief's "Paper Noise", which is licensed for personal use only) as the only font on every text; gradient background and ambient spore particles on every screen; rounded UI kit (panel, round icon button, toggle). *Decided 2026-10-24: Shantell Sans, Bold and SemiBold, spacing −2, as static font assets; a world-space `Backdrop` under each scene's camera, pausing with the level; the kit drawn for the project ([ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) and [ADR-0013](adr/0013-pause-the-level-and-handle-back.md) Refinements).* | ADR-0012, ADR-0013 | Every existing screen renders in the new look; SetPass and overdraw checked against the budget. *Done 2026-10-24: rendered at 1080 × 2400 and measured on the phone; the backdrop adds 2–3 SetPass, so level 11's pause screen is at 12 (budget ≤ 10), to be brought back in slice 4 ([performance budget](performance-budget.md)).* |
| 2 | **Scene fade**: screens fade out and in on every scene change; input is blocked during the fade. *Found 2026-10-24: one quick tap on Play also starts the level whose button sits under the same spot on level select (the tap carries across the scene change); the fade's input block must fix it, with a test.* *Decided 2026-10-25: the backdrop's gradient (first its deep blue), 0.25 s each way, the first screen fades in too; a request while the screen changes is ignored ([ADR-0010](adr/0010-hand-written-composition-root.md) Refinements).* | ADR-0010 | Every transition fades; nothing can be pressed mid-fade. *Done 2026-10-25: on the phone the fade shows, and the tap that used to start level 8 opens level select only (3 runs out of 3).* |
| 3 | **Spores and board**: circles told apart by outline (smooth, scalloped, spiked, notched), no cell tiles, medium size; grayscale and colour-blindness simulation check. *Decided 2026-10-25: the mockup's colours (red, purple, green, orange), compared first with Okabe–Ito and a tuned set; notched as the fourth outline, not a ring; keys renamed after the outlines; 0.68 of a cell across, the smooth spore 0.64 so all four look the same size ([ADR-0005](adr/0005-identify-spores-by-asset-with-runtime-id.md) and [ADR-0011](adr/0011-fit-camera-to-board-and-pool-views-up-front.md) Refinements).* | ADR-0005, ADR-0011 | The four types stay distinct in every simulation; the silhouette validation still guards them. *Done 2026-10-25: rendered at 1080 × 2400 in normal vision, protanopia, deuteranopia, tritanopia and grayscale; the guard now compares the sprites' pixels.* |
| 4 | **Gameplay HUD**: goals and moves left together at the top; settings and pause buttons; pause modal (Resume, Quit). *From slice 1: the goal slots must batch, bringing a four-goal level back to SetPass ≤ 10, paused included.* *Decided 2026-10-25: one card for moves and goals at the top, under a "Level N" tab, and a pause button below the board; Resume, Restart and Quit (to level select); the settings button comes with its modal in slice 5 ([ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) and [ADR-0013](adr/0013-pause-the-level-and-handle-back.md) Refinements).* | ADR-0012, ADR-0013 | A level plays with the new HUD; pause and back still behave; SetPass ≤ 10 on the phone. *Done 2026-10-25: level 11 measured SetPass 8 playing and 10 paused; the pause button paused the level on the phone (scripted taps).* |
| 5 | **Settings modal**, shared by the main menu, level select and gameplay: sound, haptics and accessibility options; saved on change. *Decided 2026-10-25: switches for sound effects, music and haptics, saved now and taking effect in slices 9–10; accessibility is a note (colour-blind support is always on); in a level the button sits beside pause, and settings opened there pause the level and resume it on close ([ADR-0006](adr/0006-save-progress-as-versioned-json.md), [ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) and [ADR-0013](adr/0013-pause-the-level-and-handle-back.md) Refinements).* | ADR-0006, ADR-0013 | Settings persist; back closes the modal; opening it mid-level pauses the level. *Done 2026-10-25 in tests and on the phone (opened over level 11: paused, SetPass 10).* |
| 6 | **Main menu and level select**: title and a slowly breathing mushroom that starts the game; "Levels" with Soni's level-node shape, help and settings buttons. *Decided 2026-10-25: the mushroom alone is Play (no button, no subtitle); settings, and on level select help, in a bottom-centre footer; help shows but does nothing until slice 8; chunky nodes with soft stars on open levels and the lock (redrawn without the bumps under it) on locked ones ([ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) Refinements).* | ADR-0012 | The menu flow works in the new design. *Done 2026-10-25: on the phone the mushroom breathes and opens level select, help does nothing, settings and back behave; main menu SetPass 6, level select 5, 0 B GC.* |
| 7 | **Level complete**: title, stars, "Interesting Fact", justified fact text, photo with its source, Continue and Retry; the loss panel restyled. *Decided 2026-10-25: a win fills the screen (board and HUD hidden) with Continue only; the short "Photo: <site>" credit under the photo's left edge; a loss is a small card over the dimmed board with three dimmed stars, Retry and Continue; the photo shrinks to fit, the fact never does; on taller phones the parts spread out and sit a little above centre; bigger stars (230) with a softer outline, on level select too ([ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) Refinements). Added 2026-10-25: a win first pops the spores in a wave from the last chain, then shows the result (a tap or back skips; [ADR-0009](adr/0009-animate-playback-with-primetween.md) Refinements); the chain line takes the spores' colour and draws behind them ([ADR-0008](adr/0008-pointer-action-and-grid-hit-testing.md) Refinements).* | §3.6, ADR-0012 | Wins and losses both work end to end. *Done 2026-10-25: on the phone Soni won level 5 and lost level 4 twice (Retry, then Continue); win SetPass 6, loss card 10.* |
| 8 | **Onboarding**: three pages (welcome, how to play, the SDGs the game relates to) with a Next button; shown on the first launch only, replayed from level select. *Decided 2026-10-26: an Intro scene opened by the first Play (until finished) and by help (?), both ending in level select; back turns back a page and leaves from the first, no skip; seen only once finished; Soni's text edited to the real rules (no points, "same kind"); "How to Grow" animates a finger drawing two chains of the game's own spores; the three UN SDG icons committed, unmodified, with the UN's disclaimer and link; no analytics event ([ADR-0006](adr/0006-save-progress-as-versioned-json.md), [ADR-0010](adr/0010-hand-written-composition-root.md) and [ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) Refinements).* | ADR-0006 | A fresh install shows it once; it replays on request. *Done 2026-10-26 on the phone (an old save, as a fresh install would be): back on the first page returned to the main menu and the next Play showed the intro again; finishing it saved it as seen and opened level select; help replayed it; Play then went straight to level select. SetPass 6–8, 0 B GC on every page.* |
| 9 | **Haptics**: a light tick per spore added to a chain, a stronger one on commit and win; follows the setting | ADR-0013 | Felt on the phone; off when disabled; 0 B per move |
| 10 | **Audio**: sound effects and music at the set volumes; pauses with the level and in the background | ADR-0013 | Sounds play and pause correctly |
| 11 | **Device pass**: the full new flow on the phone | Performance budget | Milestone 4 done-when met on the mid-range phone |

> **Decided before starting (2026-10-24):** an OFL lookalike font instead of Paper Noise (shortlist rendered, Soni picks); the list of assets to draw for the project approved (four outline spores, round icon buttons, a glow dot for the particles, modal panel, goal pill, toggle, a softer star; the gradient made in code); the mushroom and level-node shape are Soni's own work; the UN SDG icons stay out of the repository until their use is checked against the UN's guidelines (checked in slice 8, then committed). **To decide in their slices:** the spore colours (decided in slice 3), the pause modal's buttons (decided in slice 4), Retry after a win (ADR-0012 Refinements decided against it), what "accessibility modes" means given that colour-blind support is always on (§6; decided in slice 5), whether the background particles pause with a level (ADR-0013), and the onboarding text (decided in slice 8).

## 9. Open questions

- [x] Neighbour rule and minimum chain length: 8-way, minimum 2 (§3.2)
- [x] Refill strategy: uniform from the level's types, plus reshuffle guarantee (§3.3)
- [x] Score: cut; 1–3 stars by moves left (§3.4)
- [ ] Star thresholds (25 % / 50 %): tune in playtesting (§3.4). *With the defaults the balancing bot never earns 3★ on levels 11–12 ([balancing.md](balancing.md)). Decided 2026-10-24 after the first phone playtest: keep the defaults until other people have played; revisit in the Milestone 5 content pass.*
- [x] Spore identity: ScriptableObject assets, runtime byte id, stable key ([ADR-0005](adr/0005-identify-spores-by-asset-with-runtime-id.md))
- [x] Save format: versioned JSON with JsonUtility, no mid-level resume ([ADR-0006](adr/0006-save-progress-as-versioned-json.md))
- [ ] Choose the 4 fungus species and design silhouettes that stay distinct at phone size and in colour-blindness simulations. *Silhouettes done 2026-10-25 (four outlines, Milestone 4 slice 3); the species names are still open.*
- [x] Main menu or straight to level select: main menu first (§4, [ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) Refinements)
- [ ] Reference low-end device: deferred to Milestone 2, since only a mid-range phone is available today (§6, performance budget). *Deferred again on 2026-10-23, to Milestone 5.*
- [x] Render pipeline: URP with the 2D Renderer, unlit sprites by default ([ADR-0002](adr/0002-use-urp-with-2d-renderer.md))
- [ ] Git LFS for binary art and audio, or keep them in plain git (the project is small)
- [ ] Which art and audio to replace with your own versus license, and where fact images come from (they need clear usage rights)

## 10. Credits and provenance

The idea was first prototyped on a third-party Asset Store template to check that it was fun to play. **All code in this repository was designed and written from scratch for this project.** Third-party art, audio and fonts are listed below with their licences. Asset Store content is kept out of the repository (`Assets/ThirdParty/` is git-ignored); the UN SDG icons are the one exception, under the UN's published terms ([ADR-0012](adr/0012-use-ugui-with-textmeshpro.md) Refinements 2026-10-26).

| Asset | Source | Licence |
|---|---|---|
| PrimeTween (code dependency, not in the repo) | npm package `com.kyrylokuzyk.primetween` by Kyrylo Kuzyk | PrimeTween licence: free in commercial products; in GitHub projects only as an npm package ([ADR-0009](adr/0009-animate-playback-with-primetween.md)) |
| Spore silhouettes (smooth, scalloped, spiked and notched circles; the placeholder shapes and the board cell tile they replaced), UI panel, star and lock | Made for this project (spores drawn by code) | Own work |
| Spore colours | Sampled from Soni Seli's mockup (Milestone 4; the Okabe–Ito palette of Okabe & Ito, 2008, before it) | Own work |
| LiberationSans font (TextMeshPro's default font asset; no longer used on any screen since Milestone 4) | Red Hat, Inc. / Google Corporation, via Unity's TextMeshPro essential resources | SIL Open Font License 1.1 (`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`) |
| Shantell Sans (the game font, Bold and SemiBold, baked into static TextMeshPro font assets) | The Shantell Sans Project Authors (Shantell Martin with Arrow Type), [github.com/arrowtype/shantell-sans](https://github.com/arrowtype/shantell-sans), release 1.011 | SIL Open Font License 1.1 (`Assets/_Mycelia/Art/Fonts/ShantellSans - OFL.txt`) |
| Mushroom (cap and stem) and level-node shape | Soni Seli | Own work |
| UI kit: round icon buttons, modal panel, goal pill, toggle, star; backdrop gradient and spore glow | Made for this project (drawn by code) | Own work |
| TextMeshPro essential resources (shaders, settings, style sheet) | Unity Technologies, from the `com.unity.ugui` package | Unity Companion License; the sample emoji set is removed |
| UN Sustainable Development Goal icons 9, 12 and 13 (the intro's last page; `Assets/_Mycelia/Art/UI/SDG/`, imported at 512 from the 3000-pixel English files) | United Nations, [un.org/sustainabledevelopment](https://www.un.org/sustainabledevelopment/news/communications-material/); terms in the UN's [SDG guidelines](https://www.un.org/sustainabledevelopment/wp-content/uploads/2023/09/E_SDG_Guidelines_Sep20238.pdf) (September 2023) | UN SDG guidelines: illustrative use without permission, icons unmodified, no implied endorsement, with the UN's disclaimer and a link to its SDG site (both on the page); **commercial use needs the UN's written permission**: check again before any store release |
| Fact card photos (12, one per level; **local only, not in the repo**) | Collected by Soni Seli from the web; source per level in [facts.md](facts.md) | **Unknown.** Kept in the git-ignored `Assets/ThirdParty/FactPhotos/`; must be licensed or replaced before any public build, store listing or README media |
