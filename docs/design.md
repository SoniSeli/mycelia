# Mycelia: Design Document

| | |
|---|---|
| **Status** | Draft |
| **Author** | Soni Seli |
| **Last updated** | 2026-10-21 |
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
- Each spore type is a **fungus species** with its own colour *and* silhouette, so types never rely on colour alone ([ADR-0005](adr/0005-identify-spores-by-asset-with-runtime-id.md)).
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
Launch → (first run) Intro tutorial → Level select ⇄ Settings
                                         │
                                         ▼
                                       Level ── win ──▶ Stars + fact card ──▶ Level select
                                         │
                                         └── lose ──▶ Retry / Level select
```

> **Decide:** Does the main menu earn its place, or should launch go straight to level select? Fewer taps to play is a common mobile choice.

## 5. Architecture (proposal)

Three layers, each depending only on the one below it:

| Layer | Contains | Unity dependency | Tested by |
|---|---|---|---|
| **Domain** | Board, chain rules, gravity/refill, reshuffle, objectives, star rating, level session state machine | None (pure C#) | EditMode unit tests |
| **Game** | Board view, spore views (pooled), chain input, HUD, panels | MonoBehaviours | PlayMode smoke tests and manual testing |
| **App** | Bootstrap, scene loading, save, audio, haptics, settings, analytics | MonoBehaviours and services | Manual testing on device |

**Data.** `SporeType` (stable key, species name, colour, unique silhouette sprite, accessible label), `SporeCatalog`, `LevelDefinition` and `LevelCatalog` are ScriptableObjects. The domain refers to spore types only by a 1-byte runtime `SporeTypeId`, and analytics uses the stable key. Neither ever uses colour ([ADR-0005](adr/0005-identify-spores-by-asset-with-runtime-id.md)). These ScriptableObjects and `LevelDefinitionMapper` (asset → `LevelConfig`) live in a fourth assembly, **`Mycelia.Content`**, between Domain and Game, because both the Game views (sprites, colours) and the App (level select, saves) need them. Its Editor assembly shows each asset's problems in the Inspector, and EditMode tests check that every level asset in the project is playable.

**Communication.** The domain is an engine-free assembly. Committing a chain runs a fixed pipeline of phases (Clear → Trigger → Damage → Gravity → Refill → Hazards → Settle → Evaluate). Each phase's rules record typed steps, and the resulting `MoveResult` is played back by the Game layer phase by phase, with input locked ([ADR-0003](adr/0003-separate-pure-domain-from-views.md), [ADR-0004](adr/0004-resolve-moves-through-a-phase-pipeline.md)). There are no global static event buses.

**Extending the game.** A new mechanic is added as new rule, step and step-player classes, plus one line registering it in the pipeline. Cells are only accessed through the `Board` API, so new cell layers (obstacles, hazards) don't change existing callers. Objectives implement `IObjective`. See the mold walkthrough in ADR-0004.

**Dependencies.** A single bootstrap scene creates the services and passes them to the scenes that need them. There are no `Resources`-loaded singletons and no static state carried between scenes.

**Rendering.** URP with the 2D Renderer; sprites are unlit unless a measured effect needs lighting ([ADR-0002](adr/0002-use-urp-with-2d-renderer.md)).

**Persistence.** One versioned `progress.json` serialized with `JsonUtility`, with golden-file migration tests and atomic writes. There is no mid-level resume ([ADR-0006](adr/0006-save-progress-as-versioned-json.md)).

**Input.** One Input System action bound to `<Pointer>` (primary touch on device, mouse in the Editor); screen positions map to cells with grid maths and the circular hit radius from §6 ([ADR-0008](adr/0008-pointer-action-and-grid-hit-testing.md)).

**Animation.** `MovePlayback` turns each `MoveResult` into one PrimeTween Sequence (steps grouped within a phase, phases chained), installed only through npm per its licence and written to stay allocation-free ([ADR-0009](adr/0009-animate-playback-with-primetween.md)).

**Wiring.** `AppRoot` in the Bootstrap scene creates the services once and passes them to each scene's single entry point; no singletons or static state ([ADR-0010](adr/0010-hand-written-composition-root.md)).

**Board view.** The board stays in world units (one cell = one unit) and the camera is fitted to it inside the safe area, leaving tunable bands for the HUD. Spore views come from a pool filled before play, and board sprites share one atlas ([ADR-0011](adr/0011-fit-camera-to-board-and-pool-views-up-front.md)).

Proposed decisions to record (see [adr/README.md](adr/README.md)): UI framework (before the Milestone 2 HUD slice).

## 6. Mobile considerations

- **Input:** touch through the Input System, with mouse as a fallback for the Editor. A second finger is ignored while dragging. Because chains can go diagonally (§3.2), a spore joins the chain only when the finger comes within a **circular** radius of its centre (about 0.4 of a cell, to be tuned on device), not anywhere in its square cell. That way a diagonal drag doesn't accidentally catch the side neighbours it passes between.
- **Screen:** portrait only. Supports aspect ratios from 16:9 to about 21:9 and tablets, with layout inside the device's safe area.
- **App lifecycle:** progress saves atomically (temporary file → replace, with a backup) when a level is won, when settings change, and when the app is paused. A level in progress is not resumed (§3.6). When the app is paused during a level, it shows a pause overlay and waits for the player. Audio pauses on interruptions. The Android back button behaves sensibly on every screen.
- **Performance:** see [performance-budget.md](performance-budget.md). The target is a low-end reference device, not the Editor.
- **Accessibility:** colour-blind support is **always on, not a setting**. Every spore type is a species with a unique silhouette, enforced by a validation test, and is checked in grayscale and in colour-blindness simulations. Text is readable at phone size. Haptics can be turned off.
- **Localisation:** _out of scope for v1?_ If so, keep fact text in data so it can be localised later.

## 7. Testing strategy

- **Domain:** EditMode tests written alongside the code. Test names read as rules, e.g. `Chain_RejectsDifferentSporeType`, `Gravity_LeavesNoGapsBelowSpores`. Boards are written as text grids (`Boards.Parse`), so a test reads like the rule it checks. One test asserts the pipeline's phase order.
- **Randomness:** the domain takes an injected `IRandom` (PCG32) seeded explicitly in every test; in the game each attempt gets a fresh, logged seed, so any board can be replayed exactly ([ADR-0007](adr/0007-random-seed-per-attempt-with-pcg32.md)).
- **Game/App:** a small number of PlayMode smoke tests (a level loads and a scripted chain resolves), plus a manual device checklist before each release.
- **CI:** every push runs the tests and builds an Android APK.

## 8. Milestones

| # | Milestone | Done when |
|---|---|---|
| 0 | Repo, docs, CI building an empty Android project | CI is green and the APK installs on the reference device. *Amended 2026-10-22: installed on the mid-range device; the low-end reference device is deferred to Milestone 2.* |
| 1 | Domain logic and tests | Every rule in §3 is covered by a passing test |
| 2 | Board view, touch input, pooling, lifecycle, safe area | Low-end reference device chosen; one level is playable on it within budget |
| 3 | Level flow, save system, analytics events | All 12 levels are playable and progress persists |
| 4 | App shell, back button, haptics, colour-blind mode | Full flow works on device |
| 5 | Content and build-size pass | Build size and memory are within budget |
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
| 6 | **HUD**: moves left, objective counters, win/lose panel (minimal) | UI framework ADR | A level can be won or lost and the result is shown |
| 7 | **Lifecycle**: pause overlay on `OnApplicationPause`, Android back button, audio pause, 30/60 fps policy | ADR-0006, ADR-0010 | Backgrounding mid-level pauses cleanly; back button behaves on every screen |
| 8 | **Device pass**: choose the low-end reference device; Profiler connection check (deferred from Milestone 0); memory investigation (the 246 MB case study); frame time, GC and batches against the budget | Performance budget | Results logged on both devices; Milestone 2 done-when met |

## 9. Open questions

- [x] Neighbour rule and minimum chain length: 8-way, minimum 2 (§3.2)
- [x] Refill strategy: uniform from the level's types, plus reshuffle guarantee (§3.3)
- [x] Score: cut; 1–3 stars by moves left (§3.4)
- [ ] Star thresholds (25 % / 50 %): tune in playtesting (§3.4)
- [x] Spore identity: ScriptableObject assets, runtime byte id, stable key ([ADR-0005](adr/0005-identify-spores-by-asset-with-runtime-id.md))
- [x] Save format: versioned JSON with JsonUtility, no mid-level resume ([ADR-0006](adr/0006-save-progress-as-versioned-json.md))
- [ ] Choose the 4 fungus species and design silhouettes that stay distinct at phone size and in colour-blindness simulations
- [ ] Main menu or straight to level select (§4)
- [ ] Reference low-end device: deferred to Milestone 2, since only a mid-range phone is available today (§6, performance budget)
- [x] Render pipeline: URP with the 2D Renderer, unlit sprites by default ([ADR-0002](adr/0002-use-urp-with-2d-renderer.md))
- [ ] Git LFS for binary art and audio, or keep them in plain git (the project is small)
- [ ] Which art and audio to replace with your own versus license, and where fact images come from (they need clear usage rights)

## 10. Credits and provenance

The idea was first prototyped on a third-party Asset Store template to check that it was fun to play. **All code in this repository was designed and written from scratch for this project.** Third-party art, audio and fonts are listed below with their licences. Asset Store content is kept out of the repository (`Assets/ThirdParty/` is git-ignored).

| Asset | Source | Licence |
|---|---|---|
| PrimeTween (code dependency, not in the repo) | npm package `com.kyrylokuzyk.primetween` by Kyrylo Kuzyk | PrimeTween licence: free in commercial products; in GitHub projects only as an npm package ([ADR-0009](adr/0009-animate-playback-with-primetween.md)) |
| Placeholder spore silhouettes (circle, triangle, square, diamond) and board cell tile | Made for this project | Own work |
| Placeholder spore colours | Okabe–Ito colour-blind-safe palette (Okabe & Ito, 2008) | Colour values, freely usable |
| _e.g. Comfortaa font_ | _Google Fonts_ | _SIL OFL 1.1_ |
