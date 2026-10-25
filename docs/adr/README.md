# Architecture Decision Records

Each file records one significant decision: its context, the options considered, the choice made and its consequences. New records start from [0000-template.md](0000-template.md) and take the next free number.

## Index

| # | Decision | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-use-urp-with-2d-renderer.md) | Use URP with the 2D Renderer | Accepted (amended 2026-10-24) |
| [0003](0003-separate-pure-domain-from-views.md) | Keep game rules in an engine-free domain; views play back move results | Accepted (refined by 0004) |
| [0004](0004-resolve-moves-through-a-phase-pipeline.md) | Resolve moves through a phase pipeline of rules that emit typed steps | Accepted |
| [0005](0005-identify-spores-by-asset-with-runtime-id.md) | Identify spore types by ScriptableObject asset, with a compact runtime id and a stable key | Accepted (amended by 0006; refined 2026-10-25) |
| [0006](0006-save-progress-as-versioned-json.md) | Save progress as a versioned JSON file; no mid-level resume | Accepted (refined 2026-10-24, 2026-10-25) |
| [0007](0007-random-seed-per-attempt-with-pcg32.md) | Use a random, logged seed per attempt, with a PCG32 generator in the domain | Accepted |
| [0008](0008-pointer-action-and-grid-hit-testing.md) | Read input through one pointer action and map it to cells with grid maths | Accepted (refined 2026-10-23, 2026-10-25) |
| [0009](0009-animate-playback-with-primetween.md) | Animate move playback with PrimeTween sequences | Accepted (refined 2026-10-23, 2026-10-24, 2026-10-25) |
| [0010](0010-hand-written-composition-root.md) | Wire services with a hand-written composition root in the Bootstrap scene | Accepted (refined 2026-10-23, 2026-10-24, 2026-10-25) |
| [0011](0011-fit-camera-to-board-and-pool-views-up-front.md) | Fit the camera to the board, pool spore views up front, and pack board sprites in one atlas | Accepted (amended 2026-10-24; refined 2026-10-25) |
| [0012](0012-use-ugui-with-textmeshpro.md) | Build the game's UI with uGUI and TextMeshPro | Accepted (refined 2026-10-23, 2026-10-24, 2026-10-25; amended 2026-10-24) |
| [0013](0013-pause-the-level-and-handle-back.md) | Pause the level when the player looks away, and handle the back button in the Game scene | Accepted (refined 2026-10-23, 2026-10-24, 2026-10-25) |
| [0014](0014-log-playtest-analytics-to-a-local-file.md) | Log playtest analytics to a local file behind an analytics interface | Accepted |
| [0015](0015-balance-levels-with-a-simulated-bot.md) | Balance levels with a simulated bot over fixed seeds | Accepted |

## Candidates to decide

These came up while drafting the [design document](../design.md). Each becomes an ADR once decided.

| Topic | The question to answer |
|---|---|
| Large binary assets | Git LFS or plain git for a small project? |
