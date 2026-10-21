# ADR-0003: Keep game rules in an engine-free domain; views play back move results

| | |
|---|---|
| **Status** | Accepted (`MoveResult` shape refined by [ADR-0004](0004-resolve-moves-through-a-phase-pipeline.md)) |
| **Date** | 2026-10-21 |
| **Deciders** | Soni Seli |

## Context

The rules in [design §3](../design.md#3-game-rules) (8-way chains, gravity, uniform refill with a reshuffle guarantee, objectives, stars) are the core of the game and the part most likely to have subtle bugs. Examples: gaps left after gravity, a reshuffle that never finds a valid chain, an objective counted twice.

In the prototype, these rules lived inside MonoBehaviours and coroutines, mixed with animation timing (`WaitForSeconds`), audio and singletons. They could only be checked by playing the game.

Two questions need answering:
1. **Where do the rules live, and how strictly are they kept away from Unity?**
2. **How does the view find out what a move did,** so it can animate pop → fall → refill → reshuffle in the right order?

The performance budget also applies: **0 B of GC allocation per frame during a move** after warm-up.

## Options considered

### Where the rules live

**A. Strict: engine-free domain assembly.** `Mycelia.Domain.asmdef` has `noEngineReferences: true`.
- **Pros:** The compiler guarantees the separation. EditMode tests run fast with no scene. Behaviour is deterministic given an injected `IRandom`.
- **Cons:** Needs its own small types (`GridPosition`, `IRandom`). ScriptableObject data must be mapped to plain config objects.

**B. Pure C#, but Unity math types allowed** (`Vector2Int`, `Mathf`).
- **Pros:** Less boilerplate.
- **Cons:** The boundary relies on discipline. It's easy for `Debug.Log`, `Time`, or `UnityEngine.Random` to creep in and make tests flaky.

**C. Rules in MonoBehaviours** (the prototype's approach).
- **Pros:** Least upfront work.
- **Cons:** Testing needs PlayMode and scenes, and rules get tangled with animation timing. It's also the weakest signal to a reviewer.

### How the view finds out what happened

**1. `MoveResult` object.** Committing a chain resolves the whole move at once and returns a description of every step.
- **Pros:** The view plays back an explicit, ordered list of steps. The domain stays synchronous and easy to test. A single object is easy to log, assert on, or replay.
- **Cons:** The result has to describe falls and spawns precisely enough to animate them.

**2. Fine-grained C# events** (`SporeRemoved`, `SporeMoved`, …).
- **Pros:** A familiar pattern.
- **Cons:** The domain finishes instantly, so the view still has to queue events to animate them in sequence. Subscription order and re-entrancy become sources of bugs.

**3. The view compares the board before and after.**
- **Pros:** The simplest domain.
- **Cons:** The view has to work out which spore fell where. That information is lost after the move, so the animations become guesswork.

## Decision

We will put the game rules in an **engine-free domain assembly** (A). Committing a move returns a **`MoveResult`** (1), which the view plays back step by step. Input stays locked until playback finishes.

The deciding factor is **correctness you can prove**. The rules are where the bugs hide, and the compiler-enforced boundary plus a single result object make every rule in §3 a fast, deterministic unit test.

### Shape (illustrative; the details are settled in Milestone 1)

> **Refined by [ADR-0004](0004-resolve-moves-through-a-phase-pipeline.md).** The decision here still stands: an engine-free domain, and a view that plays back a result. ADR-0004 changes *how* the result is modelled. `Commit` runs a phase pipeline of rules, and `MoveResult` becomes an ordered list of phases containing typed steps, instead of the fixed fields below. The view plays phases in sequence and the steps within a phase together. The sketch below is kept as the original proposal.

```csharp
// Mycelia.Domain: noEngineReferences = true
public readonly struct GridPosition { public readonly int Column, Row; }
public interface IRandom { int Next(int maxExclusive); }

public sealed class LevelSession
{
    public LevelSession(LevelConfig config, IRandom random);

    public IBoardView Board { get; }        // read-only view of spore types per cell
    public int MovesRemaining { get; }
    public SessionState State { get; }      // Playing, Won, Lost

    // Used while dragging: validates neighbour, same type, not already in the chain
    public bool CanExtend(IReadOnlyList<GridPosition> chain, GridPosition next);

    // Used on release: resolves the whole move (remove → gravity → refill → reshuffle → objectives → outcome)
    public MoveResult Commit(IReadOnlyList<GridPosition> chain);
}

// Ordered description of one move, for playback and tests
public sealed class MoveResult
{
    public IReadOnlyList<GridPosition> Cleared { get; }
    public IReadOnlyList<Fall> Falls { get; }            // from → to, per spore
    public IReadOnlyList<Spawn> Spawns { get; }          // cell + spore type (+ which slot above the board it drops from)
    public IReadOnlyList<Relocation> Reshuffle { get; }  // empty if no reshuffle was needed
    public IReadOnlyList<ObjectiveProgress> Objectives { get; }
    public int MovesRemaining { get; }
    public SessionState Outcome { get; }
    public int Stars { get; }                            // 0 unless Outcome == Won
}
```

### Assemblies

| Assembly | References | Contains |
|---|---|---|
| `Mycelia.Domain` | none (`noEngineReferences: true`) | Board, chain rules, gravity, refill, reshuffle, objectives, stars, `LevelSession` |
| `Mycelia.Domain.Tests` | Domain, NUnit (EditMode) | One test fixture per rule |
| `Mycelia.Game` | Domain, Unity | `BoardView`, pooled `SporeView`s, `ChainInputController`, playback, HUD |
| `Mycelia.App` | Game, Domain, Unity | Bootstrap, services, mapping ScriptableObjects → `LevelConfig` |

## Consequences

- **Positive:**
  - Every rule in §3 can be covered by fast EditMode tests with a seeded `IRandom`, so boards and bugs can be reproduced exactly.
  - Animation timing, audio and haptics live only in the view. Changing them can't break the rules.
  - `CanExtend` puts the drag rules (8-way neighbours, same type, no repeats, backtracking) in the domain too, so input behaviour is tested, not just move resolution.
  - A logged sequence of `MoveResult`s is enough to reproduce a reported bug, and later could power replays or a level-solver tool.
- **Negative:**
  - A mapping layer is needed: `LevelDefinition` (ScriptableObject) → `LevelConfig` (plain C#), and `GridPosition` ↔ world position in the view.
  - The view must play results back faithfully. Mitigation: in development builds, after playback, the view checks that its spores match `Board` and logs an error if they don't.
  - **Allocations:** a new `MoveResult` per move would break the 0 B GC budget. The session therefore **reuses one `MoveResult` and its internal lists**, clearing them on each `Commit`. A result is only valid until the next `Commit`. Callers must not keep a reference to it, and this is documented on the type.
- **Follow-up:**
  - Milestone 1: create the assemblies above, then test-drive `LevelSession` rule by rule from §3.
  - Settle the spore identity ADR (how `SporeTypeId` maps to ScriptableObjects) before writing the mapping layer.
  - Settle the animation flow ADR (coroutines vs `Awaitable`) before writing playback in Milestone 2.
  - **Revisit if:** playback mismatches show up repeatedly in development builds. That would suggest `MoveResult` is missing information.
