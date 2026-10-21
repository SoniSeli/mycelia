# Architecture Decision Records

Each file records one significant decision: its context, the options considered, the choice made and its consequences. New records start from [0000-template.md](0000-template.md) and take the next free number.

## Index

| # | Decision | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-use-urp-with-2d-renderer.md) | Use URP with the 2D Renderer | Accepted |
| [0003](0003-separate-pure-domain-from-views.md) | Keep game rules in an engine-free domain; views play back move results | Accepted (refined by 0004) |
| [0004](0004-resolve-moves-through-a-phase-pipeline.md) | Resolve moves through a phase pipeline of rules that emit typed steps | Accepted |
| [0005](0005-identify-spores-by-asset-with-runtime-id.md) | Identify spore types by ScriptableObject asset, with a compact runtime id and a stable key | Accepted (amended by 0006) |
| [0006](0006-save-progress-as-versioned-json.md) | Save progress as a versioned JSON file; no mid-level resume | Accepted |

## Candidates to decide

These came up while drafting the [design document](../design.md). Each becomes an ADR once decided.

| Topic | The question to answer |
|---|---|
| Dependency wiring | Hand-written bootstrap, a DI framework (VContainer or Zenject), or singletons? |
| Animation flow | Coroutines, `async`/Awaitable, or a tween library for the pop → fall → refill sequence? |
| Input handling | Input System touch API vs. pointer events vs. legacy Input. How are multi-touch and Editor testing handled? |
| Randomness | Should the injected `IRandom` use a fixed seed per level (reproducible boards) or a random one? |
| Large binary assets | Git LFS or plain git for a small project? |
