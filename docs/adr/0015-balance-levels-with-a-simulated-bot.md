# ADR-0015: Balance levels with a simulated bot over fixed seeds

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-24 |
| **Deciders** | Soni Seli |

## Context

Milestone 3 needs 12 playable levels with a difficulty curve (design §3.6, §8.2 slice 7). A level can vary in board size, moves, spore types in play and "collect N of type X" goals; boards are rectangles. Tuning 12 levels by playing them takes many plays per change, and one person's results swing with luck and skill. The domain is pure C# and every board comes from an explicit seed (ADR-0003, ADR-0007), so a level can be played thousands of times in seconds outside Unity's player.

## Options considered

### Option A: Draft a curve and check it with a simulated player
- **Pros:** Numbers for every change: win rate, star spread and moves left per level over hundreds of seeds, reproducible exactly. The tool stays for later content changes.
- **Cons:** A bot isn't a person: it measures difficulty relative to other levels, not a player's experience. Needs code and tests.

### Option B: Port the prototype's levels
- **Pros:** The prototype's 12 levels were playtested.
- **Cons:** Refill, stars and spore identity changed since (design §3), so its balance doesn't carry over.

### Option C: Hand-tuned curve, playtest only
- **Pros:** No tooling.
- **Cons:** Many plays per change, with results swayed by luck.

## Decision

We will **draft the curve and check it with `GreedyBot` and `LevelSimulator`**, in an Editor-only assembly `Mycelia.Balancing` that depends only on the domain and never ships. **Deciding factor:** decisions about difficulty should rest on numbers, and a seeded simulation gives repeatable numbers for every level in minutes.

- **The bot:** every move it takes the longest chain of a spore type a goal still needs, or the longest chain of any type when none can be made; it searches each group for its longest path (with a step budget) and never looks ahead.
- **The run:** seeds 1..n per level (500 for the results in [balancing.md](../balancing.md)); a report lists win rate, the share of 1/2/3-star wins and moves left. Menu *Mycelia → Simulate Levels*, or headless `BalancingRun.RunBatch`.
- **Target bands** rise from "always won, mostly 3★" (levels 1–3) to "won 65–85 % by the bot, 3★ rare" (10–12), set high because the bot plays better than a casual player.
- **Rectangles only for now.** *Why:* board shapes need a grid on the level asset, mapping, validation and a check that the view and refill handle holes: a slice of its own, better once the 12 levels play well.

## Consequences

- **Positive:**
  - Every content change can be checked in minutes, and the numbers are part of the docs.
  - The bot doubles as a soak test: thousands of full games through the real pipeline, which would surface a soft-lock or an exception.
- **Negative:**
  - The bot plays above a casual player (in the first playtest a person lost a level the bot always wins); the bands make up for it, but they are a guess until playtests confirm them.
- **Follow-up:**
  - Slice 8: playtest on the phone and compare with the bot's ranking.
  - Open: star thresholds on levels 11–12 (the bot never earns 3★ there with the defaults).
  - **Revisit if:** levels gain shapes or new goal types (the bot must learn them), or playtests rank levels very differently from the bot.
