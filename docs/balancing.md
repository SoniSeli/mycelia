# Level balancing

How the 12 levels were tuned (design §8.2 slice 7). Levels vary by board size, moves, spore types in play and goals; boards are rectangles for now.

## Method

`Mycelia.Balancing` (Editor-only, pure C#) plays each level with **`GreedyBot`**: every move it takes the longest chain of a spore type a goal still needs, or the longest chain of any type when none can be made. It plans one move at a time and never looks ahead. **`LevelSimulator`** plays a level once per seed (seeds 1..n, so a run can be reproduced exactly, ADR-0007) and counts wins, stars and moves left. Run it from *Mycelia → Simulate Levels*, or headless with `-executeMethod Mycelia.Content.Editor.BalancingRun.RunBatch` (env `SIM_SEEDS`, `BALANCE_OUT`).

The bot sees the whole board and never misses a chain, so it plays better than a casual player (in the first playtest a person lost a level the bot always wins). Its win rates **rank** levels by difficulty; they don't predict a person's. The target bands are set high for that reason.

| Levels | Bot target |
|---|---|
| 1–3 | win ~100 %, 3★ in at least 60–70 % of wins |
| 4–6 | win ≥ 95 %, 3★ 30–60 % |
| 7–9 | win 85–95 %, 3★ 15–35 % |
| 10–12 | win 65–85 %, 3★ 5–20 % |

What tuning showed: when every type in play is a goal, any chain counts, so those levels need large targets; a single big goal is the hardest kind per spore; 7×7 boards give long chains (about 8 spores a move).

## Results (2026-10-24)

Spore types in play are the first *n* of the catalog: circle, triangle, square, diamond. Star thresholds are the defaults (★★ at 25 %, ★★★ at 50 % of moves left).

Bot: greedy, one move ahead. 500 seeds per level (1..500).

| Level | Board | Moves | Types | Goals | Bot wins | ★ / ★★ / ★★★ (of wins) | Moves left (wins) |
|---|---|---|---|---|---|---|---|
| level-01 | 6×6 | 15 | 3 | 10 circle | 100 % | 0 % / 0 % / 100 % | 12.5 |
| level-02 | 6×6 | 15 | 3 | 12 circle + 12 triangle | 100 % | 0 % / 3 % / 97 % | 10.9 |
| level-03 | 6×6 | 16 | 4 | 15 square | 100 % | 2 % / 31 % / 67 % | 8.6 |
| level-04 | 6×6 | 16 | 4 | 14 circle + 14 diamond | 100 % | 9 % / 60 % / 31 % | 6.4 |
| level-05 | 7×7 | 18 | 4 | 18 triangle + 18 square | 100 % | 15 % / 52 % / 32 % | 7.3 |
| level-06 | 6×6 | 18 | 4 | 15 circle + 15 triangle | 100 % | 8 % / 59 % / 33 % | 7.6 |
| level-07 | 7×7 | 17 | 4 | 19 circle + 19 triangle + 19 square | 95 % | 34 % / 50 % / 16 % | 5.7 |
| level-08 | 6×6 | 18 | 4 | 24 diamond | 97 % | 43 % / 47 % / 11 % | 5.1 |
| level-09 | 7×7 | 20 | 4 | 23 triangle + 23 square + 23 diamond | 93 % | 41 % / 47 % / 13 % | 5.5 |
| level-10 | 6×6 | 17 | 4 | 18 circle + 18 triangle + 18 square | 87 % | 63 % / 31 % / 5 % | 3.8 |
| level-11 | 7×7 | 20 | 4 | 40 circle + 40 triangle + 40 square + 40 diamond | 89 % | 86 % / 14 % / 0 % | 2.7 |
| level-12 | 6×6 | 17 | 4 | 27 circle + 27 triangle + 27 square + 27 diamond | 86 % | 90 % / 10 % / 0 % | 2.4 |

Levels 8, 11 and 12 sit a few points above their win band. With the default thresholds the bot never earns 3 stars on levels 11–12 (see the playtest below for what was decided).

## Playtest on the phone (2026-10-24, Milestone 3 slice 8)

Soni played all 12 levels once, in order, on the mid-range phone (a development build of 5fd71dd). Every attempt's seed is in `events.jsonl` (ADR-0014), so the bot could play the **same seeds**: the same starting board, though the boards differ after the first move because refills depend on the chain.

| Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Soni: moves left (stars) | 14 (3) | 13 (3) | 9 (3) | 7 (2) | 7 (2) | 8 (2) | 6 (2) | 12 (3) | 9 (2) | 8 (2) | lost, then 1 (1) | 0 (1) |
| Bot on the same seed | 14 | 13 | 11 | 7 | 4 | 3 | 1 | 10 | 5 | 4 | 4, then 3 | lost |
| Bot over 500 seeds (mean, wins) | 12.5 | 10.9 | 8.6 | 6.4 | 7.3 | 7.6 | 5.7 | 5.1 | 5.5 | 3.8 | 2.7 | 2.4 |

What it showed:

- **A player who plans ahead beats the greedy bot on levels 5–10**, by 3–7 moves on the same seed: the bot never sets up a chain for its next move. The bot's ranking still holds for the order of the levels, but it overstates how hard the middle levels are for such a player.
- **Levels 11–12 are a step up:** a loss and a 1-move win on level 11, a win on the last move of level 12, 1 star on both. With four goals there, any chain counts, and the bot did about as well as Soni.
- **Levels 1–2** take 1–2 moves for both (one long chain on the first board).

Decided 2026-10-24: **keep the curve and the default star thresholds** until other people have played it, and revisit in the Milestone 5 content pass. Soni's feel: 11–12 are a "hard but fair finale". The deciding factor: it matches that feel, and one play-through by the person who designed the levels is too little to tune on. *Decided 2026-10-27 (design §8.4): no playtest round for now, so the curve and the default thresholds stay. The deciding factor (Soni): this is a portfolio prototype, and tuning difficulty for players isn't what it needs to show.*
