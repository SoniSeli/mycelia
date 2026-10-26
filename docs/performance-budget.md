# Performance Budget

| | |
|---|---|
| **Status** | Targets set; measured on the mid-range device (Milestone 2 slice 8); the low-end reference device and the lines not yet measured move to Milestone 5 |
| **Last updated** | 2026-10-24 |

> **Why budgets first.** These targets are set *before* the code is written, so every system is designed to fit them. Measurements on a real device are recorded against them below, and any significant fix gets a short case study.

## Reference devices

| Role | Device | SoC / RAM | Android | Why this device |
|---|---|---|---|---|
| **Low-end (budget target)** | _To choose in Milestone 5_ | _e.g. Helio G35 / 3 GB_ | _e.g. 11_ | _Represents the bottom of the audience. If it's fine here, it's fine everywhere._ |
| Mid-range (sanity check) | Xiaomi Redmi Note 12 Pro+ 5G (22101316UG) | MediaTek Dimensity 1080 (MT6877V) / 8 GB, 1080 × 2400 | 13 (API 33) | Developer's own phone, available every day. Good for checking that things work, too fast to prove the budgets. |

> **Status (2026-10-22): no low-end device yet; deferred to Milestone 2.** Only the mid-range phone is available. Until a low-end device is chosen, results on the mid-range phone are **provisional**: a pass there doesn't prove the budget, but a failure there (like the memory result below) is a real problem. Options for Milestone 2: a cheap second-hand phone (the most useful, because you can profile it yourself), or a device farm such as Firebase Test Lab for start-up and crash checks on low-end models.
>
> **Decided 2026-10-23 (Milestone 2 slice 8): measure on the mid-range phone only for now; the low-end device moves to Milestone 5.** The deciding factor: only the Redmi is available, and buying a test phone isn't worth it before there is content to measure. Every result below therefore stays provisional.

## Budgets

All figures are measured on the **low-end reference device** with a **Development build** and the Profiler attached, unless noted otherwise. Until Milestone 5 they come from the mid-range phone, with `PerfProbe` instead of a live Profiler (see How to measure).

> **Not measured yet; moved to Milestone 5 (2026-10-24):** memory growth over 10 levels, cold start to level select, the 15-minute session, overdraw, and scripts-only CPU time. They need the finished levels, the level select or the low-end device. The live Profiler connection moves there too.

### Runtime

| Metric | Budget | Notes |
|---|---|---|
| Frame rate during gameplay | **Stable 60 fps** (16.6 ms per frame) | `Application.targetFrameRate = 60` |
| Frame rate on menus and idle screens | **30 fps** | Saves battery and reduces heat |
| CPU main thread: gameplay scripts | **≤ 2 ms per frame** | Domain logic and views, excluding rendering |
| CPU main thread: total | **≤ 10 ms per frame** | Leaves room for spikes and heat throttling |
| GC allocations during a move (input → pop → fall → refill) | **0 B per frame** after warm-up | Pooling; no LINQ or boxing in code that runs every frame. Warm-up means each code path has run once in this run of the app (see Changes) |
| Longest single frame during a move | **≤ 25 ms** | No visible hitch |
| Render thread | **≤ 2 ms per frame (median)** | Replaces "batches ≤ 30" (see Changes) |
| SetPass calls (gameplay scene) | **≤ 10** | Shader-state changes; sprite atlas, shared materials |
| Overdraw | Full-screen transparent layers ≤ 2 | Mobile GPUs are limited by fill rate |

### Memory

| Metric | Budget | Notes |
|---|---|---|
| Total app memory (PSS) | **≤ 300 MB** | Measured with `adb shell dumpsys meminfo` on a release build. Was 200 MB (see Changes) |
| Texture memory | **≤ 60 MB** | ASTC compression, atlases, no unused mipmaps on UI |
| Audio memory | **≤ 10 MB** | Short SFX decompressed on load; music streamed |
| Memory growth over 10 levels played in a row | **≤ 5 MB** | Checks for leaks between scene loads |

### Size and startup

| Metric | Budget | Notes |
|---|---|---|
| Download size (Play Store, per device ABI) | **≤ 40 MB** | AAB, IL2CPP ARM64, managed code stripping |
| Cold start to level select being interactive | **≤ 3 s** | Measured from tapping the app icon |
| Level load (level select → playable board) | **≤ 500 ms** | |

### Thermal and battery

| Metric | Budget | Notes |
|---|---|---|
| 15-minute play session | **No thermal throttling warning; frame rate stays within budget** | Use Adaptive Performance if available on the device |

> **Decide:** Adjust any number you can't justify. A budget you can explain ("16.6 ms per frame is 60 fps, and I keep 6 ms of headroom for heat throttling") is worth more than an ambitious one.

### Changes

- **2026-10-23: "Draw calls / batches ≤ 30" became render thread ≤ 2 ms (median) and SetPass calls ≤ 10.** URP's SRP Batcher draws every sprite as its own batch but keeps the shader state, so the board shows 77 batches with only 6 SetPass calls. On the Redmi, those 77 batches cost the same as 6 batches with the SRP Batcher off (case study below). The deciding factor: the budget should measure the cost it exists to protect, not a count that the SRP Batcher makes misleading. The numbers are today's measurements (6 SetPass, 0.5 ms) with headroom for a few more materials; the median, because single idle frames already reach 2 ms and spikes are covered by the longest-frame budget.
- **2026-10-23: total memory went from ≤ 200 MB to ≤ 300 MB.** An empty Unity 6 + URP scene already uses about 246 MB on the Redmi, and the game 271 MB, almost all of it fixed engine and display overhead (case study below). The deciding factor: 200 MB can't be met by any work on the game's own content, and 300 MB leaves about 30 MB for 12 levels of art, fonts and audio (about 50 MB with High stripping) while staying comfortable on 3 GB phones.
- **2026-10-24: "after warm-up" means once each code path has run once in this run of the app.** On the phone, the first move, the first goal pulse and the first touch of each level allocate a few KB: IL2CPP sets up each method and runs static constructors on first call, PrimeTween's assertions (development builds only) build comparers once, and uGUI's EventSystem sets up its pointer state on a level's first touch (case study below). The deciding factor: these costs come from IL2CPP and the engine, happen once, and are far too small to trigger a GC on their own; prewarming them away would add code that exists only for the measurement. Costs that come back with every level in *our* code are not warm-up: they're fixed, and guarded by tests.
- **2026-10-24: a cost shown to exist only in development builds is judged on release-build timing.** In the Milestone 3 play-through, moves on 7 × 7 boards took 31–46 ms on the main thread in a development build. The time was spent building the move's PrimeTween sequence, and the same step takes 0.45–2.2 ms in a release build (case study below). The deciding factor: players only ever run release builds, so a hitch that exists only in development builds isn't one they can see, and rewriting playback to avoid it would add code that exists only for the measurement. Such a cost is measured with a release-build timer, or with `PerfProbe` in a development build that defines `PRIME_TWEEN_DISABLE_ASSERTIONS` for that build only.
- **2026-10-23: managed code stripping stays at Minimal for now.** High saves 19 MB of memory and 5.5 MB of APK, but can remove code reached only through reflection. The deciding factor: memory is within the new budget without it, and the Milestone 5 size pass is the place to adopt it with a full play test.

## How to measure

| What | Tool | Method |
|---|---|---|
| Frame, main-thread, render-thread and GPU time; GC; batches; SetPass | `PerfProbe` (development builds only) | Logs one line every 300 frames: `adb logcat -s Unity \| findstr [Perf]`. Times from the FrameTimingManager (Frame Timing Stats on), GC, batches and SetPass from ProfilerRecorders. Play 10 moves and read the windows that cover them. The deciding factor for a probe: it can be rerun over adb after every change, without someone at the Editor. |
| The same, as screenshots | Unity Profiler (connected to device) | Development build, connected over USB. Record 10 moves; screenshots of the CPU module and GC Alloc column go to `docs/perf/`. *The Redmi's USB link drops under the live stream, so the live connection moved to Milestone 5. Until then: a capture recorded on the phone (`Profiler.enableBinaryLog`), pulled in checksummed pieces and loaded in the Editor; the Profiler keeps at most 2,000 frames (case study below).* |
| Comparing builds | Profile Analyzer | Compare captures before and after a change; report median and 95th-percentile frame time. |
| Memory | Memory Profiler package + `adb shell dumpsys meminfo <package>` | Capture after loading a level and again after 10 levels. |
| Draw calls / batches | Frame Debugger | Gameplay scene, mid-move. |
| Build size | Build Report (Editor log) + Play Console size estimate | After every release build. |
| Startup | Stopwatch or `adb logcat` (Displayed time), 3 runs, take the median | Cold start: force-stop the app first. |

Store raw captures outside the repo (they're large). Commit screenshots of the relevant views to `docs/perf/` instead.

> **Measure memory on release builds, or with the Profiler attached.** A development build with *Autoconnect Profiler* that can't reach the Editor buffers up to 128 MB of profiler data in memory (`Profiler.maxUsedMemory`), which inflates PSS. On 2026-10-22 the same empty scene measured 350 MB PSS as a development build and 246 MB as a release build.

## Results log

| Date | Build / commit | Device | Metric | Budget | Measured | Pass | Notes |
|---|---|---|---|---|---|---|---|
| 2026-10-22 | ebe4851 (empty Bootstrap scene) | — | APK size, release, arm64 | ≤ 40 MB | **33.2 MB** | ✅ (83 % used) | Engine and generated code, not content: `libil2cpp.so` 15.1 MB (61.5 MB unpacked), `libunity.so` 8.4 MB, IL2CPP metadata 2.3 MB, `classes.dex` 2.2 MB. Managed stripping at its default; unused engine modules still enabled. Baseline for Milestone 5. |
| 2026-10-22 | ebe4851 (empty Bootstrap scene) | Mid-range | Cold start → activity displayed | ≤ 3 s *to level select* | 216 / 228 / 346 ms (median **228 ms**) | n/a | Measures `am start -W` TotalTime only; there is no level select yet. |
| 2026-10-22 | ebe4851 (empty Bootstrap scene) | Mid-range | Total app memory (PSS), release | ≤ 200 MB | **246 MB** | ❌ | Graphics 66 MB, Code 63 MB, native heap 19 MB, 10 s after launch. Over budget before any content. Suspects: full-resolution (1080 × 2400) swapchain and URP intermediate targets (Graphics); unstripped IL2CPP code mapped in memory (Code). To investigate with the Memory Profiler in Milestone 2. |
| 2026-10-22 | 7334e24 (domain only) | Editor (Mono) | GC alloc per move (domain: Commit) | 0 B | **0 B** | ✅ (Editor) | Automated: `AllocationTests.Commit_DoesNotAllocateAfterWarmUp` (Unity `Is.Not.AllocatingGCMemory`) on a 6 × 6 board after 5 warm-up moves; shown to fail when Commit allocates one array. Covers the game rules only: input, views and animation are measured on device in Milestone 2. |
| 2026-10-23 | 31f10fa + probe, development | Mid-range | Frame time during moves | Stable 60 fps | median and p95 **16.5 ms** in every window | ✅ | `PerfProbe`, about 10 moves played by hand. |
| 2026-10-23 | 31f10fa + probe, development | Mid-range | Longest frame during a move | ≤ 25 ms | **24.8 ms** | ✅ (tight) | Once, during one of the first moves; first-use costs, like the warm-up allocations below. |
| 2026-10-23 | 31f10fa + probe, development | Mid-range | GC per frame during moves, real HUD | 0 B after warm-up | **0 B** in 9 windows of play after about 22 KB over the first moves | ✅ (this run) | Input, playback and the real HUD (TextMeshPro's player-only `SetText` path, ADR-0012). Showing the pause overlay the first time allocates about 22 KB (not during a move). |
| 2026-10-23 | slice 8, development | Mid-range | GC per frame during moves, real HUD (second run, 3 levels) | 0 B after warm-up | small allocations in 8 of about 25 windows of play: 24, 72, 72, 120, 384 B, and **1,332 B** at the start of each new level | ❌ | Source not yet known (game code, input or engine): to find with allocation call stacks in the Profiler. Play again's scene load allocates 200–220 KB, outside a move. *Traced and fixed on 2026-10-24: see the rows below.* |
| 2026-10-23 | slice 8, development | Mid-range | Longest frame during a move (second run) | ≤ 25 ms | **25.9 ms** once, mid-game; otherwise ≤ 21.6 ms | ❌ (by 0.9 ms) | Cause not yet known; for the Profiler session. Play again's scene load takes one 36–39 ms frame (level load, not a move). *Explained on 2026-10-24 (display waits) and re-measured at 24.6 ms: see the rows below.* |
| 2026-10-24 | slice 8, development, Profiler capture | Mid-range | Sources of the allocations during play | 0 B after warm-up | Two per-level costs in our code (564 B on each level's first move; 24 B per pooled spore view on its first use) and first-use costs in IL2CPP, PrimeTween, uGUI and the Input System | ✅ after fixes | Our two are fixed and guarded in EditMode (`TheFirstMoveOfANewLevel_DoesNotAllocate`, `ShowingViewsThatWereNeverRentedBefore_AllocatesNothing`); the rest is warm-up (see Changes). Re-measured on the phone in the next row. |
| 2026-10-24 | 3cf8714, development | Mid-range | GC per frame during moves, after the fixes (2 levels, Restart between) | 0 B after warm-up | Level 1: about 21.5 KB over the first moves (warm-up, 860 B less than before), then **0 B**. Level 2: **544 B** on the first touch, then **0 B** in all 8 windows of play | ✅ | The 860 B saved matches the `Reshuffler` (564 B) plus about 12 pool views (24 B each). Level 2's 544 B is uGUI's EventSystem on the level's first touch (warm-up). Longest frame during moves **24.6 ms** (✅). The first pause overlay (18 KB) and Restart's scene load (207 KB) aren't moves. |
| 2026-10-24 | slice 8, development, Profiler capture | Mid-range | What the long frames are | ≤ 25 ms | captured long frames up to **22 ms**, all waiting on the GPU and display (`Gfx.WaitForPresentOnGfxThread`, `Device.Present`); no script over 1 ms | ✅ (watch) | The 25.9 ms frame itself wasn't captured. Decided to record it as most likely a missed display refresh and keep watching; the deciding factor: every long frame captured was waiting on the display, with no script time, so there is nothing in our code to change. Rechecked on the low-end device. |
| 2026-10-23 | slice 8, development | Mid-range | SetPass, render thread, main thread during moves | ≤ 10; ≤ 2 ms; ≤ 10 ms | SetPass **6–8**; render median **0.5–0.9 ms**; main median **2.3–4.1 ms** | ✅ | Main, render and GPU times rose together after about 25 s of play, before any scene reload: likely CPU clocks, not a leak. Batches up to 93. |
| 2026-10-23 | slice 8, development | Mid-range | CPU main thread (idle board) | ≤ 10 ms total | median **2.3 ms**, max 4–6 ms | ✅ | `cpuMainThreadFrameTime`, which already leaves out the wait for Present (main 2.3 + wait 14.2 ≈ 16.5 ms). Scripts alone (≤ 2 ms) aren't separated by the probe. |
| 2026-10-23 | slice 8, development | Mid-range | Render thread (idle board) | ≤ 2 ms median | median **0.5–0.6 ms**, max 2.0 ms | ✅ | |
| 2026-10-23 | slice 8, development | Mid-range | SetPass calls / batches (idle board) | ≤ 10 SetPass | **6** SetPass, 77 batches | ✅ | GPU median 4.7 ms. With the SRP Batcher off: 6 batches, same main, render and GPU times. |
| 2026-10-23 | slice 8, release | Mid-range | Total app memory (PSS), release | ≤ 300 MB | 272 / 270 MB (median of 4 cold starts **271 MB**) | ✅ (was ❌ against 200 MB) | Unknown 93, GL mtrack 56–58, `.so` mmap 70, native heap 28 MB. Unity's counters (development build): textures 8.8 MB, render textures 0. |
| 2026-10-23 | slice 8, release | Mid-range | APK size, release, arm64 | ≤ 40 MB | **36.6 MB** | ✅ (91 % used) | Managed stripping High would bring it to 31.1 MB. |
| 2026-10-24 | 5fd71dd, development (Milestone 3 slice 8) | Mid-range | Frame time during moves, all 12 levels | Stable 60 fps | median **16.5 ms** in every window of play | ✅ | `PerfProbe` over a full play-through: 13 levels played in about 18 minutes, with two app restarts. |
| 2026-10-24 | 5fd71dd, development | Mid-range | Longest frame during a move | ≤ 25 ms | **31–46 ms** on the 7 × 7 levels (5, 7, 9, 11), about once a minute; up to 29 ms on the 6 × 6 levels | ❌ (development builds only) | No GC in those windows. Two Profiler captures traced it to building the move's PrimeTween sequence (up to 25 ms), which is slow only in development builds: see the next row and the case study. |
| 2026-10-24 | 5fd71dd + temporary timer, release | Mid-range | Building a move's playback (`MovePlayback.Play`) | part of ≤ 25 ms | **0.45–2.2 ms** per move (13 moves on level 9, 18–52 steps each) | ✅ | The same code took 13–25 ms on big moves in a development build. Judged on release timing (see Changes). |
| 2026-10-24 | 5fd71dd, development | Mid-range | GC per frame during moves | 0 B after warm-up | **0 B** in every window of play without a scene load | ✅ | Loading a level allocates 270–570 KB over 4–16 frames; launch 296 KB. |
| 2026-10-24 | 5fd71dd, development | Mid-range | Level end (save, `level_end` event, result card) | not a move | **450–480 KB** GC in 1–2 frames; longest frame there **28.3 ms** | ✅ (watch) | Cause of the allocation not traced (the file writes, the result card's text, or both). The main-thread saves and event writes stay on the watch list. |
| 2026-10-24 | 5fd71dd, development | Mid-range | SetPass, render thread during play | ≤ 10; ≤ 2 ms median | SetPass **6–10** (10 with four goals on the HUD); render median **0.8–1.0 ms** | ✅ | Batches up to 124 during big moves. One 10 s window in level 11 showed **210 batches and 16 SetPass** with no pause overlay open; cause unknown, not seen in two later captures (watch). |
| 2026-10-24 | 5fd71dd, development | Mid-range | Menus (main menu, level select) | 30 fps; ≤ 10 SetPass | **33 ms** frames (30 fps); main menu 3 batches, 3–4 SetPass; main median 1.9–3.4 ms; 0 B GC | ✅ | Level select was only seen in windows mixed with other screens. Loading the Game scene takes one 38–44 ms frame, and a cold start one 134 ms frame (neither is a move). |
| 2026-10-24 | 5fd71dd, release, local build with the 12 photos | Mid-range | Total app memory (PSS) | ≤ 300 MB | 287 / 285 MB; after compressing the photos **266 / 264 MB** | ✅ | GL mtrack 69 → 59 MB: all 12 photos load with the level catalog at launch. CI builds have no photos. |
| 2026-10-24 | 5fd71dd, release, local build with the 12 photos | — | APK size, release, arm64 | ≤ 40 MB | **43.0 MB**; after compressing the photos **38.7 MB** | ❌ → ✅ | Seven photos whose sides aren't multiples of 4 shipped uncompressed (Android's automatic ETC2 needs 4-pixel blocks), 0.7–2.0 MB each. Now max 1024 px, ASTC 6 × 6 on Android: about 2.75 MB for all 12. Done now rather than in Milestone 5; Soni's reason: it's a cheap fix. |
| 2026-10-24 | Milestone 4 slice 1, development | Mid-range | SetPass and render thread with the new backdrop (gradient and spores on every screen) | ≤ 10; ≤ 2 ms median | Main menu SetPass **6–7** (was 3–4); level select **5**; a one-goal level **8** (was 6); level 11 (four goals) **10** playing and **12** paused. Render median 0.4 ms on the menus, 0.6–1.0 ms in the game | ❌ (level 11 paused) | The backdrop adds 2–3 SetPass: its gradient and its particle system, which doesn't use the SRP Batcher (sharing one material changed nothing). Four goals on the HUD add 2 more than one. Decided (Soni): commit the slice and bring the HUD back within budget in slice 4, re-measured there; the budget itself is unchanged. |
| 2026-10-25 | Milestone 4 slice 4, development | Mid-range | SetPass, level 11 (four goals) with the new HUD: pause button and one card | ≤ 10 | **8** playing, **10** paused (pause opened from the HUD button); 0 B GC while playing; render median 0.5–0.9 ms | ✅ | First build measured 9 / 11: the Moves caption sat on the HUD canvas and the numbers on the counters canvas, so the same font drew twice. With every HUD text on the counters canvas, one draw (ADR-0012 Refinements). Opening the pause allocates about 34 KB in 2 frames (not a move). |
| 2026-10-25 | Milestone 4 slice 5, development | Mid-range | SetPass, level 11 with the settings modal open (the level paused under it) | ≤ 10 | **10** with settings open, **8** playing; 0 B GC while playing | ✅ | Settings replace the pause modal while open, so the two never draw together. Opening them allocates about 85 KB in 2 frames (not a move). |
| 2026-10-25 | Milestone 4 slice 6, development | Mid-range | Menus in the new design: main menu with the breathing mushroom (its own canvas), level select with chunky nodes and a footer, settings open over level select | 30 fps; ≤ 10 SetPass; 0 B GC | Main menu SetPass **6**, level select **5**, settings open **7**; **33.1 ms** frames; main median 2.5 (menu) and 3.2–3.8 ms (level select); render median 0.4–0.6 ms; **0 B** GC in every steady window | ✅ | The mushroom's own canvas added no SetPass (6, slice 1 measured 6–7). The breathing allocates nothing. Changing scene allocates 520 KB over 4 frames and opening settings 97 KB (not a move, as before). Main median on the menu read 4.1 ms after a return from level select (2.5 ms at first); not traced. |
| 2026-10-25 | Milestone 4 slice 7, development | Mid-range | Result screens: the full-screen win (board and HUD hidden, level 5's fact with its photo) and the loss card over the dimmed board (level 4), Soni playing | ≤ 10 SetPass; 0 B GC on a still screen | Win **6** SetPass (7 batches), loss card **10** (level 4 playing: 9); **16.5 ms** frames, main median 3.9 ms, render median 0.6–0.8 ms; **0 B** GC on both screens after they open | ✅ (loss at the limit) | Hiding the board and HUD makes the win cheaper than play. Showing the result allocates 350 KB over 2 frames for a win (save + event + photo card; the Milestone 3 level end measured 450–480 KB) and 25 KB in 1 frame for a loss. The result screens run at 60 fps like the level, not at the idle screens' 30 fps (as before this slice). Windows holding a scene change reached SetPass 11 for a moment (the fade over the game). Level 11 (four goals) wasn't lost on the phone: its loss card may land at 10–11. |
| 2026-10-26 | Milestone 4 slice 8, development (`noasserts`) | Mid-range | The intro: page 1, page 2 with the chain demo running, page 3 with the SDG icons (a replay from level select, scripted taps) | 30 fps; ≤ 10 SetPass; 0 B GC on a still screen | Page 1 **6** SetPass, page 2 **8** (23 batches: the demo's spores and line are sprites, outside the SRP Batcher), page 3 **6**; **33.1 ms** frames; main median 2.4 (page 1) and 4.1 ms (page 2, max 10); render median 0.4–0.7 ms; GPU ~4.9 ms; **0 B GC** on every page, the demo included | ✅ | Turning a page allocates 220–320 KB in 2 frames (cause not traced; likely the next page's texts and layout built as it first shows; not a move, added to the watch list). The first run from the main menu behaved the same on the phone (steps checked by screenshot); its Perf lines were lost to a dropped log capture. |

## Optimisation case studies

Add one section per meaningful fix. Two or three good ones are enough.

### 77 batches and a budget measuring the wrong thing

- **Symptom:** the probe showed 77 batches on the idle board, against a budget of 30. The board is 36 cells and 36 spores from one atlas, in correct sorting order, so it should batch.
- **Measurement before:** 77 batches, 6 SetPass calls; main thread 2.3 ms, render thread 0.6 ms, GPU 4.7 ms (medians, idle, Redmi).
- **Hypothesis:** URP's SRP Batcher draws every renderer as its own batch and only keeps shader state between them, so the batch count no longer reflects cost. Dynamic batching would merge the sprites but can't run while the SRP Batcher does.
- **Experiment:** three development builds, measured the same way. Dynamic batching on: 77 batches, no change. SRP Batcher off: **6 batches**, main 2.3 ms, render 0.5 ms, GPU 4.6 ms, the same as before.
- **Change:** no code change. The budget limits render-thread time (≤ 2 ms median) and SetPass calls (≤ 10) instead of batches; the SRP Batcher stays on.
- **Trade-off:** the budget no longer matches the familiar "draw calls" figure, so it needs this explanation. To be rechecked on the low-end device, where per-draw costs are higher.

### 246 MB before any content: fixed overhead

- **Symptom:** the empty scene measured 246 MB PSS in Milestone 0, and the game 271 MB, against a 200 MB budget.
- **Breakdown (release, `dumpsys meminfo`):** Unknown 93 MB (anonymous, private: Unity's native allocators), GL mtrack 56–58 MB (Vulkan at 1080 × 2400), `.so` mmap 70 MB (`libil2cpp`/`libunity`, mostly clean pages), native heap 28 MB. Unity's own counters: textures 8.8 MB, GC heap 5 MB, render textures 0. The game's content is a small part.
- **Hypotheses and experiments** (release builds, two cold starts each, same session):

  | Variant | PSS (MB) | Change |
  |---|---|---|
  | Baseline | 271.2 | |
  | 2D renderer without depth/stencil buffer (no sprite masks are used) | 271.5 | none: on this Mali GPU it is likely tile memory |
  | Vulkan swapchain 2 buffers instead of 3 | 271.9 | none: the buffers belong to the system compositor |
  | Unused post-processing shader variants stripped | 271.8 | none |
  | Managed code stripping High | **252.1** | −19 MB (code −10, Unknown −3.5, native −2); APK −5.5 MB |

- **Change:** the memory budget went to 300 MB (see Changes). Stripping High waits for the Milestone 5 size pass and a full play test.
- **Trade-off:** a looser budget, which now guards content growth (about 30 MB of headroom) rather than promising a small footprint; the fixed overhead is measured and explained instead.

### Allocations during play, found without a live Profiler

- **Symptom:** the probe showed small allocations during play (24 B to 1.3 KB), some at the start of every level, and one 25.9 ms frame. The probe can't say where they come from.
- **Obstacle:** the phone's USB link drops under the live Profiler's stream ("device offline"), and under large transfers in general.
- **Method:** a temporary change made the development build record a Profiler capture on the phone (`Profiler.enableBinaryLog`, with allocation call stacks). The file was pulled in 16 MB pieces, each checked against a checksum on the phone and retried until it matched, then joined and checked as a whole. A headless Editor script loaded it (`ProfilerDriver.LoadProfile`) and walked every frame (`RawFrameDataView`): each `GC.Alloc` with its parent markers and call stack, and the longest frames with their most expensive markers. The Profiler only keeps 2,000 frames, so the capture that answered the question covered the first 1,900 frames from launch.
- **Findings:**
  - **Our code, once per level:** `ReshuffleRule` built its `Reshuffler` lazily, on each level's first move (564 B), and each pooled `SporeView` created its C# `Transform` wrapper the first time a move showed it (24 B per view, until all 108 had been used; this explains the 24 B multiples the probe saw).
  - **First use, once per run:** IL2CPP method setup and static constructors in PrimeTween (first sequence, first goal pulse), comparers built by PrimeTween's assertions (development builds only), uGUI's EventSystem and the Input System on the first touch.
  - **Long frames:** waiting on the GPU and display, not scripts.
- **Change:** the `Reshuffler` is built with the session (`RulePipeline.For(config, board)`), and the pool reads each view's `transform` while the level loads. Both have EditMode guards, which fail on the old code.
- **Measurement after (probe, 2 levels):** 0 B per frame during moves after the first ones, in both levels; a new level's first touch went from 1,332 B to 544 B (uGUI's EventSystem only).
- **Trade-off:** none in the game; the budget's "after warm-up" is now spelled out (see Changes).

### A 25 ms move that only development builds see

- **Symptom:** in the Milestone 3 play-through, `PerfProbe` showed main-thread times of 31–46 ms about once a minute on the 7 × 7 levels, with no GC in those windows; the 6 × 6 levels stayed at or below 29 ms.
- **Method:** a temporary `PerfProbe` change recorded the Profiler on the phone in 900-frame files while the Game scene was active, and logged each file's slowest frame. Only the files that caught a long frame were pulled (3 of 14), which avoided hoping that one 2,000-frame capture would land on a big move.
- **Findings:**
  - The long frames were inside `ChainInputController.Update` (24–29 ms): the frame where the finger lifts and the move starts.
  - Not the rules: an Editor timing of `LevelSession.Commit` over 2,176 bot moves gave at most 0.02 ms, even for 30-spore chains.
  - A second capture with temporary `ProfilerMarker`s split a 31 ms frame: `MovePlayback.Play` 24.9 ms, of which `Sequence.Insert` 11.2 ms while spawning new spores, 7.8 ms placing falling spores (mostly inserts) and 3.3 ms popping the chain; renting and showing pooled views took 0.7 ms.
  - **Cause:** PrimeTween 1.4.11's `Sequence.Insert` finds the sequence's end by walking every tween already in it (`GetLast`). Building a move of *n* tweens therefore costs on the order of *n*², and a big 7 × 7 move has about 150. Development builds also compile PrimeTween's assertions (`UNITY_ASSERTIONS`), which run inside that walk.
- **Measurement in a release build** (temporary timer): building a move's playback takes **0.45–2.2 ms** (13 moves on level 9). The separate share of the assertions and of development-build code generation wasn't measured.
- **Change:** none in the game. The longest-frame line is judged on release timing for a cost shown to be development-only (see Changes).
- **Trade-off:** the *n*² growth stays. Bigger boards or more tweens per spore would make it grow; if a release build ever shows it, the fix is fewer, larger tweens, or one tween that drives our own arrays. Development builds keep showing long frames on big moves unless built without PrimeTween's assertions.

### Case study template: _title_

- **Symptom:** What did you notice, and where? Include a profiler screenshot.
- **Measurement before:** Number(s) on the reference device.
- **Hypothesis:** What you believed the cause was, and why.
- **Change:** What you changed (link the commit or PR).
- **Measurement after:** The same number(s), measured the same way.
- **Trade-off:** What the fix cost (memory, complexity, flexibility).
