# ADR-0004: Resolve moves through a phase pipeline of rules that emit typed steps

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-21 |
| **Deciders** | Soni Seli |
| **Refines** | [ADR-0003](0003-separate-pure-domain-from-views.md): replaces its illustrative `MoveResult` shape |

## Context

[ADR-0003](0003-separate-pure-domain-from-views.md) puts the rules in an engine-free domain and has the view play back a `MoveResult`. Its sketch had a single `Commit` that resolves everything, plus a `MoveResult` with a fixed field for each kind of change (`Cleared`, `Falls`, `Spawns`, `Reshuffle`, …).

That works for v1, but it doesn't scale to the mechanics puzzle games usually add over time: obstacles under or over spores, special spores, spreading hazards, wildcards, new objective types. Each would add:
- branches inside `Commit`, which turns into a god method,
- a field to `MoveResult`, which ripples into the view's playback code,
- a special case in objective tracking.

The view also needs to know **what animates together**. All falls play at once, but falls must wait for the pops to finish.

Constraints still apply: 0 B of GC allocation per frame during a move ([performance budget](../performance-budget.md)), and every rule must stay unit-testable.

## Options considered

### Option A: Single `Commit` method with a fixed-field `MoveResult` (ADR-0003 sketch)
- **Pros:** The least code for v1, and the easiest to read today.
- **Cons:** Every new mechanic edits `Commit`, `MoveResult` and the view's playback. Rule order is buried in code instead of being explicit and tested.

### Option B: Phase pipeline of rules, with `MoveResult` as an ordered list of typed steps grouped by phase
- **Pros:** A new mechanic means new rule, step and step-player classes, plus one registration line. Phase order is explicit and testable. Phases double as the animation grouping.
- **Cons:** More types up front, and one level of indirection when reading how a move resolves.

### Option C: General effect or event graph (or ECS-style systems) with reflection-based discovery
- **Pros:** Maximum flexibility.
- **Cons:** Far beyond what a 12-level puzzle game needs. Harder to debug, reflection costs on mobile, and the rule order becomes implicit.

## Decision

We will use **Option B**. Committing a chain runs an **ordered pipeline of phases**. Each phase runs its registered **rules**, and rules record what they did as **typed steps**. `MoveResult` is the ordered list of phases and their steps. The view plays **phases in sequence, and the steps within a phase together**.

The deciding factor is the rule of thumb **"build it now only if it's cheap today and painful to change later."** The step-based result costs about the same as the fixed one today, but changing the result shape after the view's playback exists would mean rewriting it. Option C fails the same test the other way round: it's expensive now for flexibility that may never be used.

### Phases (fixed order)

| # | Phase | v1 rules | Example future rules |
|---|---|---|---|
| 1 | `Clear` | `ClearChainRule` | `MoldClearRule`, break locks on cleared spores |
| 2 | `Trigger` | none | Special spores (row clear, bomb) |
| 3 | `Damage` | none | Damage obstacles next to cleared cells |
| 4 | `Gravity` | `GravityRule` | Blockers that stop falling spores |
| 5 | `Refill` | `RefillRule` (uniform from the level's types) | Spawn collectibles |
| 6 | `Hazards` | none | `MoldSpreadRule` |
| 7 | `Settle` | `ReshuffleRule` (guarantees a valid chain) | none |
| 8 | `Evaluate` | `MoveCounterRule`, `ObjectivesRule`, `OutcomeRule` (win/lose and stars) | none |

Rules within a phase run in registration order. The pipeline is built for each level in **one place** (`RulePipeline.For(LevelConfig)`), and a test asserts the phase order.

### Shape (illustrative; details settled in Milestone 1)

```csharp
public enum MovePhase { Clear, Trigger, Damage, Gravity, Refill, Hazards, Settle, Evaluate }

public interface IMoveRule
{
    MovePhase Phase { get; }
    void Apply(MoveContext context);   // reads/writes the board, records steps
}

public sealed class MoveContext
{
    public Board Board { get; }
    public IRandom Random { get; }
    public IReadOnlyList<GridPosition> Chain { get; }
    public MoveScratch Scratch { get; }               // per-move facts shared between rules, e.g. "mold cleared this move"
    public void Record<TStep>(in TStep step) where TStep : struct, IMoveStep;
}

public interface IMoveStep { }                        // marker; steps are small structs
public readonly struct SporeCleared  : IMoveStep { public readonly GridPosition Cell; public readonly SporeTypeId Type; }
public readonly struct SporeFell     : IMoveStep { public readonly GridPosition From, To; }
public readonly struct SporeSpawned  : IMoveStep { public readonly GridPosition Cell; public readonly SporeTypeId Type; public readonly int DropSlot; }
public readonly struct BoardReshuffled : IMoveStep { /* index into a reused relocation buffer */ }
public readonly struct ObjectiveProgressed : IMoveStep { public readonly int ObjectiveIndex, Current, Target; }
public readonly struct SessionEnded  : IMoveStep { public readonly SessionState Outcome; public readonly int Stars; }

public sealed class MoveResult                         // reused across moves (see ADR-0003 consequences)
{
    public int PhaseCount { get; }
    public MovePhase GetPhase(int phaseIndex);
    public StepRange GetSteps(int phaseIndex);       // ordered, allocation-free iteration
    public bool TryGet<TStep>(StepRef reference, out TStep step) where TStep : struct, IMoveStep;
}
```

**Allocation-free storage.** Each step type has its own reused, type-specific buffer (`StepBuffer<TStep>`), created the first time that type is recorded (that is, during warm-up). The ordered sequence holds lightweight `StepRef { bufferId, index }` entries. Clearing a result resets the counts without freeing memory. Step structs avoid boxing and per-move allocations, so the result stays within the 0 B budget.

**Playback in the view.** `MovePlayback` walks the phases in order. For each phase it starts a player for every step at once, using a registry keyed by step type (`IStepPlayer<TStep>`), and waits for all of them to finish before starting the next phase. A step type with no registered player is logged in development builds and skipped.

**Cells and objectives.**
- **Cells are encapsulated.** Rules go through the `Board` API (`IsPlayable`, `GetOccupant`, `SetOccupant`, …), never the cell's fields directly. In v1 a cell holds only a *void* flag and an *occupant* (spore type). Later layers (under-layer such as mud or mold, over-layer such as a lock or ice) can be added without changing any existing caller.
- **Objectives use an interface from day one:** `IObjective` updates its progress from the steps recorded in a move. v1 ships one implementation, `CollectSporesObjective`. In the Editor, levels author objectives as a `[SerializeReference]` list so new objective types can be authored without changing the level asset format.
- **Deferred:** chain rules for `CanExtend` (wildcards, locked spores) stay a single method in v1. They'll be pulled out into an `IChainRule` set when the first mechanic needs it. That refactor stays inside `CanExtend`, so deferring is cheap.

## Consequences

- **Positive:**
  - Adding a mechanic is mostly additive: new rule(s), step type(s), a step player, optionally an objective, and tests. See the walkthrough below.
  - Rule order is a visible, tested table instead of the order of lines inside one method.
  - Phases are also the animation grouping, so the domain and view agree on timing without sharing any timing code.
  - Each rule can be tested in isolation, and the whole pipeline end to end, using text-grid board fixtures (see below).
- **Negative:**
  - More types than a single `Commit` method, and reading a full move means following the pipeline table.
  - Rules communicate through `MoveScratch`, which could become a dumping ground. Mitigation: each entry is a typed field with an owner noted in a comment, and rules don't keep state between moves.
  - Steps are only valid until the next `Commit`. The view must copy anything it needs to keep (already the case in ADR-0003).
- **Follow-up:**
  - Milestone 1: implement the v1 rules in the table above, test-driven, plus a pipeline-order test and the text-grid fixture helper.
  - Milestone 2: implement `MovePlayback` and step players for `SporeCleared`, `SporeFell`, `SporeSpawned`, `BoardReshuffled`, `ObjectiveProgressed` and `SessionEnded`.
  - **Revisit if:** a mechanic needs to repeat phases (for example, cascades that re-run Clear → Gravity). The pipeline would then loop phases until the board settles, with a cap on iterations. That is an extension of this design, not a replacement.

## Testing pattern: text-grid fixtures

Boards in tests are written as text, one character per spore type (`.` = empty, `#` = void), with the bottom row last:

```csharp
var board = Boards.Parse(@"
    R G G
    R R B
    B G R");                                                   // (0,0) is the bottom-left B
var result = session.Commit(Chain.Of((0,2), (0,1), (1,1)));   // three R spores
// '.' matches any newly spawned spore
Assert.That(Boards.Print(session.Board), Is.EqualTo(@"
    . . G
    . G B
    B G R").Using(Boards.IgnoreSpawned));
```

These read as specifications, make failures obvious, and match the rule descriptions in [design §3](../design.md#3-game-rules).

## Example: adding a mold mechanic

_Hypothetical, not in v1 scope. This shows how the design absorbs a new mechanic._

**Rule (in plain words):** some cells start with **mold** underneath their spore. Clearing a spore on a mold cell removes the mold. After any move that clears no mold, the mold spreads to one random neighbouring cell. Objective: _clear all mold_.

| Piece | New code | Existing code changed |
|---|---|---|
| Data | `Mold` under-layer in the cell; `LevelDefinition` gets the starting mold cells | `Cell` gains an under-layer field, hidden behind the `Board` API, so no callers change |
| Rules | `MoldClearRule` (`Clear` phase): removes mold under cleared cells and records `MoldCleared`, and sets `Scratch.MoldClearedThisMove`. `MoldSpreadRule` (`Hazards` phase): if nothing was cleared, spreads to one random neighbour without mold and records `MoldSpread` | One line each in `RulePipeline.For(...)`, active only when the level has mold |
| Steps | `MoldCleared { Cell }`, `MoldSpread { From, To }` | None |
| Objective | `ClearAllMoldObjective`: complete when the board's mold count is 0 | None (`[SerializeReference]` list) |
| View | `MoldStepPlayer` (fade out, creeping growth). The under-layer sprite is shown via an `ICellLayerRenderer` | `BoardView` initial render: draw cell under-layers (once; later layers reuse it) |
| Tests | `MoldTests`: cleared mold, spread when not cleared, no spread when cleared, spread never targets void or already-moldy cells, objective completion, pipeline order still holds | None |

Gravity, refill, reshuffle, input, the HUD and `LevelSession` are unchanged. The only edits to existing code are pipeline registration and one under-layer rendering hook in `BoardView`, which the first layered mechanic needs anyway and every later one reuses.
