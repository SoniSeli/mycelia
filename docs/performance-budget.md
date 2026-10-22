# Performance Budget

| | |
|---|---|
| **Status** | Draft: targets proposed; mid-range device measured, low-end reference device not yet chosen |
| **Last updated** | 2026-10-22 |

> **Why budgets first.** These targets are set *before* the code is written, so every system is designed to fit them. Measurements on a real device are recorded against them below, and any significant fix gets a short case study.

## Reference devices

| Role | Device | SoC / RAM | Android | Why this device |
|---|---|---|---|---|
| **Low-end (budget target)** | _To choose in Milestone 2_ | _e.g. Helio G35 / 3 GB_ | _e.g. 11_ | _Represents the bottom of the audience. If it's fine here, it's fine everywhere._ |
| Mid-range (sanity check) | Xiaomi Redmi Note 12 Pro+ 5G (22101316UG) | MediaTek Dimensity 1080 (MT6877V) / 8 GB, 1080 × 2400 | 13 (API 33) | Developer's own phone, available every day. Good for checking that things work, too fast to prove the budgets. |

> **Status (2026-10-22): no low-end device yet; deferred to Milestone 2.** Only the mid-range phone is available. Until a low-end device is chosen, results on the mid-range phone are **provisional**: a pass there doesn't prove the budget, but a failure there (like the memory result below) is a real problem. Options for Milestone 2: a cheap second-hand phone (the most useful, because you can profile it yourself), or a device farm such as Firebase Test Lab for start-up and crash checks on low-end models.

## Budgets

All figures are measured on the **low-end reference device** with a **Development build** and the Profiler attached, unless noted otherwise.

### Runtime

| Metric | Budget | Notes |
|---|---|---|
| Frame rate during gameplay | **Stable 60 fps** (16.6 ms per frame) | `Application.targetFrameRate = 60` |
| Frame rate on menus and idle screens | **30 fps** | Saves battery and reduces heat |
| CPU main thread: gameplay scripts | **≤ 2 ms per frame** | Domain logic and views, excluding rendering |
| CPU main thread: total | **≤ 10 ms per frame** | Leaves room for spikes and heat throttling |
| GC allocations during a move (input → pop → fall → refill) | **0 B per frame** after warm-up | Pooling; no LINQ or boxing in code that runs every frame |
| Longest single frame during a move | **≤ 25 ms** | No visible hitch |
| Draw calls / batches (gameplay scene) | **≤ 30** | Sprite atlas, shared materials |
| Overdraw | Full-screen transparent layers ≤ 2 | Mobile GPUs are limited by fill rate |

### Memory

| Metric | Budget | Notes |
|---|---|---|
| Total app memory (PSS) | **≤ 200 MB** | Measured with `adb shell dumpsys meminfo` and the Memory Profiler |
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

## How to measure

| What | Tool | Method |
|---|---|---|
| Frame time, CPU, GC allocations | Unity Profiler (connected to device) | Development build with Autoconnect Profiler enabled. Record 10 moves; read the GC Alloc column and the longest frame from the CPU module. |
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

## Optimisation case studies

Add one section per meaningful fix. Two or three good ones are enough.

### Case study template: _title_

- **Symptom:** What did you notice, and where? Include a profiler screenshot.
- **Measurement before:** Number(s) on the reference device.
- **Hypothesis:** What you believed the cause was, and why.
- **Change:** What you changed (link the commit or PR).
- **Measurement after:** The same number(s), measured the same way.
- **Trade-off:** What the fix cost (memory, complexity, flexibility).
