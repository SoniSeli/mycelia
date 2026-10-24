# ADR-0014: Log playtest analytics to a local file behind an analytics interface

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-24 |
| **Deciders** | Soni Seli |

## Context

Milestone 3 needs analytics events (design §8, §8.2 slice 6). Their first job is balancing the 12 levels in playtests: which levels are won or lost, with how many moves left, and how often a player gives up. ADR-0006 also asks for `save_recovered` / `save_reset` events, and for a way to tell a level the player left apart from one lost to the app being killed. The game is a single-player portfolio piece with no accounts and no backend, played by a handful of testers on their own phones.

## Options considered

### Option A: Local file, echoed to logcat in development builds
- **Pros:** Nothing leaves the phone, so there is no consent prompt, privacy policy or Play data-safety declaration to write yet. A playtest's events can be pulled with adb as one file. No package, no network calls, testable with a temporary folder.
- **Cons:** No dashboards; data from several testers has to be collected by hand.

### Option B: Logcat only, in development builds
- **Pros:** The least code.
- **Cons:** Release builds record nothing, and a development build's events are only seen if logcat is captured live.

### Option C: Unity Analytics (Unity Gaming Services)
- **Pros:** Real dashboards and funnels across many players.
- **Cons:** A project link, a consent prompt before collection starts, a privacy policy and the Play data-safety form; another package and network calls in a game that otherwise has none.

## Decision

We will log events through an **`IAnalytics` interface** on `AppServices`, implemented by **`FileAnalytics`**, which appends one JSON line per event to `events.jsonl` in the app's storage folder and echoes it to the log in development builds. **Deciding factor:** nothing leaves the phone, so no consent or privacy work is needed yet, and a real backend can plug into the same interface later.

### Events

| Event | When | Fields |
|---|---|---|
| `app_start` | Once per launch, after the save loads | `version`, `save` (`none`, `loaded`, `recovered`, `reset`, `newer_version`) |
| `save_recovered` | The save was loaded from its backup (ADR-0006) | — |
| `save_reset` | No save file could be read; progress started over | — |
| `level_start` | A level attempt starts | `level` (stable key), `seed` (hex, ADR-0007) |
| `level_end` | Once per attempt (below) | `level`, `outcome` (`won`, `lost`, `abandoned`), `stars`, `moves_used`, `moves_left` |

Every line also carries `t` (UTC time) and `session` (a random id per launch, so one session's events can be told apart; it says nothing about the player).

- **A level the player leaves ends as `abandoned`** (Restart or Level select from the pause overlay). *Why:* a `level_start` with no end then means the app was killed mid-level, as ADR-0006's follow-up wants to measure, not that the player chose to leave.
- **`level_end` is written when the deciding move has finished playing**, with the save, so no file work lands on a move frame. If the player leaves while that move is still playing, its real outcome is written on the way out. Only a crash during that second loses it.
- **No duration.** *Why:* interruptions, notifications and players thinking make play time a weak signal for a turn-based puzzle; moves used and left say more.
- Screen views (`main_menu`, `level_select`) are not logged: they would add noise for 12 levels and little information.

### Details

- The file is capped at 256 KB; when full it moves to `events.old.jsonl` (replacing the older one) and a new file starts, so at most about 512 KB is kept.
- Logging never throws: a failed write logs a warning and the game carries on.
- JSON is written by hand (JsonUtility has no dictionaries): text values are escaped, whole numbers and flags are written as such.

## Consequences

- **Positive:**
  - Playtest data with no privacy surface: no identifiers, no network, nothing to consent to.
  - One interface for every event, faked in tests; `level_end` is checked for every way an attempt can end.
- **Negative:**
  - Data from several testers has to be pulled from each phone and merged by hand.
  - The file is written on the main thread at level start and end (under 300 bytes each); to be checked in the device pass with the save writes.
- **Follow-up:**
  - Slice 8: pull `events.jsonl` after a play-through on the phone.
  - **Revisit if:** the game is released to players we can't reach (then a backend with consent, Option C or similar, behind the same interface), or a question needs time per level.
