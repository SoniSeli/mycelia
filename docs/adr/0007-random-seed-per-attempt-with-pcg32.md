# ADR-0007: Use a random, logged seed per attempt, with a PCG32 generator in the domain

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-22 |
| **Deciders** | Soni Seli |
| **Related** | [ADR-0003](0003-separate-pure-domain-from-views.md) (engine-free domain), [design §3.3](../design.md#33-resolving-a-move) (uniform refill) |

## Context

The starting board and every refill ([design §3.3](../design.md#33-resolving-a-move)) are random. Two questions follow:

1. **What do players get?** The same board for everyone on a given level, or a fresh one on every attempt?
2. **Where do the random numbers come from?** [ADR-0003](0003-separate-pure-domain-from-views.md) keeps the domain engine-free, so `UnityEngine.Random` is not an option. The domain receives an `IRandom`.

What matters: retries should feel worth playing (stars reward replays), any bug or reported board must be reproducible exactly, tests must be deterministic, and the generator must not allocate during a move (0 B GC budget).

## Options considered

### Seed policy
- **A. Random seed per attempt, logged.** Fresh board on every attempt; the seed is logged so any board can be replayed exactly.
  - *Pros:* replays feel fresh; full reproducibility through the seed.
  - *Cons:* difficulty varies slightly between attempts.
- **B. Fixed seed per level.** Same board and refills for every player and every retry.
  - *Pros:* identical, hand-tunable difficulty.
  - *Cons:* retries turn into memorisation; with uniform refills, an unlucky sequence is unlucky for everyone.
- **C. Fixed start, random refills.** Seeded or authored opening, random mid-game.
  - *Pros:* consistent opening.
  - *Cons:* two random streams to manage, for little gain over A.

### Generator
- **D. Own small PRNG (PCG32) in the domain.**
  - *Pros:* the same seed gives the same sequence on every platform and runtime, by our own code rather than by a library's implementation detail; allocation-free; the state is two numbers, easy to save if resume is ever added.
  - *Cons:* about 30 lines to own, plus reference-value tests.
- **E. `System.Random(seed)`.**
  - *Pros:* nothing to write.
  - *Cons:* the exact seeded sequence is a .NET implementation detail, not a contract, which is a risk as Unity moves to a newer .NET runtime; its state can't be saved.

## Decision

We will use **A** and **D**: every level attempt gets a **fresh 64-bit seed**, which is **logged**, and all randomness in a session comes from a **PCG32** generator in `Mycelia.Domain`, behind `IRandom`.

The deciding factor is **reproducibility without memorisation**: players see a fresh board each time, but any board a player or a test reports can be replayed exactly from its seed.

### Details

- **One stream per session.** A `LevelSession` owns one `IRandom`, created from the attempt's seed. The starting board and every refill and reshuffle draw from it **in a fixed, documented order** (board fill: column by column, bottom to top; refills: the same order; reshuffle: Fisher–Yates over the same cell order). Same seed + same moves ⇒ same game.
- **Generator:** PCG32 (`XSH RR` output, 64-bit state, 64-bit stream selector), as published by M. E. O'Neill (pcg-random.org). Bounded integers use PCG's rejection method to avoid modulo bias.
- **Seed source (App layer):** a 64-bit value from `System.Security.Cryptography.RandomNumberGenerator` when a level starts. The domain never chooses its own seed.
- **Logging:** the seed is shown as 16 hex characters in the development overlay, included in the `level_start` analytics event, and written to the log in development builds.
- **Tests:** domain tests always pass explicit seeds. Board fixtures (`Boards.Parse`) bypass randomness entirely when a test needs an exact board.

```csharp
// Mycelia.Domain
public interface IRandom
{
    /// <summary>Uniform integer in [0, maxExclusive). maxExclusive must be > 0.</summary>
    int Next(int maxExclusive);
}

public sealed class Pcg32 : IRandom   // ~30 lines, no allocations after construction
{
    public Pcg32(ulong seed, ulong stream = 54);
    public uint NextUInt();
    public int Next(int maxExclusive);
}
```

## Consequences

- **Positive:**
  - Every bug report or odd board can be replayed exactly from its seed, and logged analytics seeds let you investigate unusually hard or easy attempts.
  - Deterministic, platform-independent tests.
  - No allocations; the generator's state could be saved if mid-level resume is revisited ([ADR-0006](0006-save-progress-as-versioned-json.md)).
- **Negative:**
  - Difficulty varies between attempts; level tuning can't rely on one specific board.
  - The draw order becomes part of the contract: changing it changes every seed's board. Tests with fixed seeds will catch an accidental change.
  - The generator is code we own and must verify.
- **Follow-up:**
  - Milestone 1: `Pcg32` with reference-value tests (the published `pcg32-demo` sequence for seed 42, stream 54) and a basic distribution check for `Next`.
  - Later (optional): a **seed-sweep test** that plays each level with a simple bot over, say, 1,000 seeds and reports the win rate, to measure how much difficulty varies and to tune moves and targets with data.
  - **Revisit if:** levels need a hand-crafted opening (e.g. a tutorial level). Option C can then be added per level without changing the generator.
