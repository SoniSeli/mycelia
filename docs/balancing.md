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

**Open:** with the default thresholds the bot never earns 3 stars on levels 11–12; whether to lower their thresholds is still to decide. Levels 8, 11 and 12 sit a few points above their win band. Playtests on the phone come next (slice 8).
