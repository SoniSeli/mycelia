# ADR-0009: Animate move playback with PrimeTween sequences

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-22 |
| **Deciders** | Soni Seli |
| **Related** | [ADR-0004](0004-resolve-moves-through-a-phase-pipeline.md) (phases of typed steps), [performance budget](../performance-budget.md) (0 B GC per move) |

## Context

`LevelSession.Commit()` returns a `MoveResult`: ordered phases of typed steps (ADR-0004). The view must play it back: **phases one after another, the steps inside a phase at the same time** (all pops together, then all falls, then all spawns). Input stays locked until playback ends.

Constraints:
- **0 B of GC allocation per move** applies to the whole move, not just the domain. The domain already passes an automated allocation test.
- Timing and easing will be tuned a lot by feel, so they should be data, not code.
- Animations must stop cleanly if the scene unloads mid-move.

## Options considered

### Option A: Our own pooled tween runner and playback state machine
- **Pros:** Zero allocations by construction, full control, no dependency.
- **Cons:** About 150+ lines of tween, easing and sequencing code to write, test and maintain, which has little to do with the game itself.

### Option B: PrimeTween (third-party tween library)
- **Pros:** Described by its author as *"allocation-free … no runtime memory allocations, ever"*. Has **Sequences** (`Chain`, `Group`, `Insert`, callbacks), cancellation, a capacity pre-allocation API, and is widely used for mobile.
- **Cons:** A third-party dependency with its own licence and update cadence. Its zero-allocation guarantee holds only if callbacks are written without capturing lambdas.

### Option C: Unity 6 `Awaitable` (async/await)
- **Pros:** Very readable sequential code.
- **Cons:** Async methods allocate when they suspend, so every move would allocate; cancellation on scene unload needs care.

### Option D: Coroutines
- **Pros:** Familiar.
- **Cons:** `StartCoroutine` allocates per call; per-frame interpolation code gets spread across views. Weakest fit for the budget.

## Decision

We will use **PrimeTween (Option B)**. `MovePlayback` builds one **Sequence per move**: each phase's step animations are **grouped** (parallel), and phases are **chained** (sequential). A final callback unlocks input.

The deciding factor is **spending the effort on the game, not on infrastructure**, while keeping the 0 B budget. PrimeTween's documented allocation-free sequences make that possible, as long as the usage rules below are followed and verified by tests.

### Installation and licence

- PrimeTween's licence allows free use in commercial games, and use in **GitHub projects only if PrimeTween is installed as an npm package**. It forbids distributing PrimeTween's source code or `.tgz` inside derivative projects.
- Therefore it is installed **only through the npm scoped registry** in `Packages/manifest.json`, pinned to an exact version:

  ```json
  "scopedRegistries": [
    { "name": "npm", "url": "https://registry.npmjs.org/", "scopes": ["com.kyrylokuzyk"] }
  ],
  "dependencies": { "com.kyrylokuzyk.primetween": "<exact version>" }
  ```

- The Asset Store version is **never** imported into this repo. Credit and licence go in design §10.

### Usage rules (to keep moves allocation-free)

1. **No capturing lambdas** in callbacks. Use the target overloads, e.g. `.OnComplete(this, playback => playback.Finish())`.
2. **No `await` or coroutines** in move playback; use Sequences (PrimeTween's documentation notes that both allocate).
3. `Tween.Custom` only through the overloads that take a target, with payloads ≤ 16 bytes.
4. **Pre-allocate capacity** at bootstrap with `PrimeTweenConfig.SetTweensCapacity(...)`, sized for the largest move (36 cells × the steps per cell, with headroom).
5. On scene unload or session end, **stop** running sequences, so no callback reaches a destroyed view.
6. Durations and easings live in a `PlaybackSettings` ScriptableObject, so they can be tuned without code changes.

### Mapping steps to animations (initial)

| Step | Animation |
|---|---|
| `SporeCleared` | scale down and fade, return the view to the pool |
| `SporeFell` | move down with a short bounce ease |
| `SporeSpawned` | appear above the board at `DropSlot` height, then fall into place |
| `SporeRelocated` | move across the board, all at once |
| `ObjectiveProgressed` | update the HUD counter with a pulse |
| `SessionEnded` | trigger the win or lose panel |

### Refinements (2026-10-23, Milestone 2 slice 5)

Decided while building playback:

- **Version:** `com.kyrylokuzyk.primetween` **1.4.11**, pinned exactly, from the npm scoped registry. 1.5.x was days old at the time.
- **Timing by insertion:** each phase starts when the previous one ends. Every step tween is `Insert`ed at its phase's start, and the phase lasts as long as its longest step. Falls take `perCell × √cells`, like gravity.
- **Steps are handled by type, in any phase.** The Settle phase can hold a reshuffle's relocations, or a regenerated board's clears and spawns. Each phase first takes every leaving or moving view off its cell, then places moved and new views. That way a cell can be emptied and refilled in the same phase, and a reshuffle can rotate spores.
- **The view's cell map is updated as the sequence is built**, to the move's end state; tweens then animate the views there. Cleared views go back to the pool in one callback at the end of their phase.
- **New spores fall in, fading in.** They start above the board at their drop slot, invisible until the refill phase, with no mask, so batching is unchanged.
- **Callbacks** use the target overloads with `static` lambdas, so the compiler rejects a capture. Tween capacity comes from `PlaybackSettings.TweenCapacity` (512) and is set in `MovePlayback.Init`, when a level loads.
- **Input lock:** `ChainDrag` hands committed moves to an `IMovePlayer` and ignores presses while one plays. A touch that begins during playback is ignored until the finger lifts, so it can't start a chain the moment playback ends (found by a headless Play check).
- **Allocation test scope:** a PlayMode test commits a move, builds its sequence and completes it at once under `Is.Not.AllocatingGCMemory()` after warm-up. That covers sequence creation and every callback. Per-frame tweening relies on PrimeTween's guarantee and the device Profiler pass (slice 8). *Slice 8 checked it with `PerfProbe` and a Profiler capture recorded on the phone; the live Profiler connection moved to Milestone 5.*
- **PlayMode tests run locally for now;** CI still runs EditMode only.

### Refinements (2026-10-24, Milestone 3 slice 8)

- **Building a sequence grows with the square of its size.** `Sequence.Insert` walks every tween already in the sequence to find its end, so a move of *n* tweens costs on the order of *n*². On the phone, a big 7 × 7 move (about 150 tweens) took up to 25 ms to build in a development build, where PrimeTween's assertions run inside that walk, and 0.45–2.2 ms in a release build ([performance budget](../performance-budget.md), case study "A 25 ms move that only development builds see").
- **Kept as it is.** The deciding factor: players only ever run release builds, so a hitch that exists only in development builds isn't one they can see, and rewriting playback to avoid it would add code that exists only for the measurement. If a release build ever shows it (bigger boards, more tweens per spore), the fix is fewer, larger tweens, or one tween that drives the views from our own arrays.


### Refinements (2026-10-25, Milestone 4)

- **A win shows itself on the board before the result.** `WinWave` sits between `LevelResultRecorder` and the HUD: moves, goals and losses pass straight through; on a win every spore left pops (1.25×, 0.36 s there and back) in a wave spreading out from the last chain's final spore (`MovePlayback.LastCleared`) at 0.06 s per cell, and 0.3 s after the last pop the HUD shows the result (about 1.2 s on a 7 × 7 board). Compared with spores floating away, a "Well done!" banner, and goal counters pulsing with the board glowing. *Why (Soni):* the player sees the win happen on the board they played before the screen changes, and the wave is clear and fits the chain-puzzle feel.
- **A tap anywhere, or back, skips to the result.** An invisible button covers the safe area on the result canvas while the wave plays (it takes taps but draws nothing: `cullTransparentMesh`), and back skips before it leaves. *Why (Soni):* replays never wait.
- The wave runs after the level has ended, when the level can no longer pause (ADR-0013), so it isn't an `IPausable`; Android stops the player loop in the background anyway. One sequence per win, within the tween capacity reserved for moves; it stops if the scene goes mid-wave (rule 5).
- **Guard narrowed:** the check that a win hides every HUD button now looks at the buttons on the HUD's own canvas, since the skip button sits on the result canvas and must stay.
## Consequences

- **Positive:**
  - Little tween code to own, with rich easing and sequencing for polish.
  - The phase structure of `MoveResult` maps directly onto Group-within-Chain sequences.
- **Negative:**
  - A third-party dependency: version pinning, update checks, and its licence terms must be respected (npm only).
  - The 0 B guarantee depends on following the usage rules. It must be **verified**, not assumed.
- **Follow-up:**
  - Milestone 2: install PrimeTween via npm, `PlaybackSettings` asset, `MovePlayback`, and a **PlayMode allocation test** that plays back a move under `Is.Not.AllocatingGCMemory()` after warm-up, plus a device Profiler check. *Done in slice 5, except the device Profiler check (slice 8).* *Slice 8 (2026-10-23): on the mid-range phone, `PerfProbe` measured about 22 KB of first-use allocations over the opening moves, then mostly 0 B per frame, but with small allocations (24 B to 1.3 KB) in some windows during play. *A Profiler capture (2026-10-24) traced them: PrimeTween's own allocations are first-use costs (IL2CPP method setup, static constructors, assertion comparers in development builds), once per run; the per-level ones were in our code and are fixed. See the [performance budget](../performance-budget.md).*
  - Add PrimeTween to design §10 (credits and licences). *Done.*
  - Tune the eases on device: `OutBack` on a 2-cell fall overshoots the target by about 0.2 of a cell. *Seen on the mid-range phone (2026-10-23) and kept as it is.*
  - Decide when PlayMode tests join CI.
  - **Revisit if:** the licence changes, the npm package stops being published, or playback allocates in a way the usage rules can't fix. Option A (own runner) is the fallback; `MovePlayback`'s interface stays the same.
