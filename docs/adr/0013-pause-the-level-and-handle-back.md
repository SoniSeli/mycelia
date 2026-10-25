# ADR-0013: Pause the level when the player looks away, and handle the back button in the Game scene

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-23 |
| **Deciders** | Soni Seli |
| **Related** | [design §6](../design.md#6-mobile-considerations) (app lifecycle), [ADR-0006](0006-save-progress-as-versioned-json.md) (no mid-level resume, pause overlay), [ADR-0008](0008-pointer-action-and-grid-hit-testing.md) (input lock), [ADR-0009](0009-animate-playback-with-primetween.md) (playback sequence), [ADR-0010](0010-hand-written-composition-root.md) (no global state, frame-rate policy), [ADR-0012](0012-use-ugui-with-textmeshpro.md) (canvas rules) |

## Context

Milestone 2 slice 7 is about the app lifecycle. ADR-0006 drops mid-level resume, so a level must never carry on while the player isn't looking: when the app is paused during a level, the game shows a pause overlay and waits. The design also asks for audio to pause on interruptions and for the Android back button to behave sensibly on every screen (design §6). The game currently has one screen, the Game scene, with a HUD and a result panel; level select arrives in Milestone 3 and the app shell in Milestone 4.

Things that can happen mid-level: the app goes to the background (home, app switch, screen off), the app loses focus while still visible (notification shade, call banner, split screen), or the player presses back. Any of these can arrive mid-drag or while a move is being played back.

## Options considered

### What the overlay offers
- **Resume only:** smallest; Restart can come with the level flow.
- **Resume + Restart:** Restart reloads the level with a fresh seed, exactly like the result panel's Play again.
- **Resume + Restart + Quit:** there is nowhere else to go yet, and Android apps rarely offer a Quit button.

### What pauses the level
- **Going to the background only** (`OnApplicationPause`), as design §6 is worded.
- **Background and focus loss** (`OnApplicationFocus(false)` as well).

### What back does
On the result panel, with no level select yet: **send the app to the background**, **play again**, or **quit** (`Application.Quit`). While playing it opens the overlay, and on the overlay it resumes, in every variant.

### How a move being played back freezes
- **Pause the move's PrimeTween sequence** in `MovePlayback`.
- **`Time.timeScale = 0`**, which freezes every scaled tween and timer at once, but is global engine state that every scene must reset and that anything moving under the overlay must opt out of.

## Decision

- **The overlay offers Resume and Restart.** Restart costs nothing to build, since it is Play again, and it is what a player who looked away mid-level most often wants.
- **The level pauses on going to the background and on losing focus.** The rule is "pause whenever the player isn't looking at the board", and the notification shade and call banners hide the board just as backgrounding does.
- **Back pauses a level in play, resumes a paused one, and on the result panel sends the app to the background.** This follows Android's convention: back steps out one layer, and on an app's first screen it leaves without killing the app.
- **`MovePlayback` pauses its own sequence.** No global engine state (ADR-0010): pausing stays inside the code that owns the animation, where it can be tested.

### Details

- **`LevelPause`** (Game, plain C#) holds the rules. A level can pause until its result is shown: while it is played, and while its last move plays back. It never resumes by itself. Pausing tells every `IPausable` it was given: `MovePlayback`, `PauseOverlay`, and the App's `PausedFrameRate`. `Back()` returns false once the result is shown, and the caller decides what back does then.
- **Input:** `ChainDrag` treats a pause as a third input lock, beside playback and the end of the level. A chain in progress is **cancelled and costs no move**, whether the finger lifts while paused or is still down. A touch that began while paused is ignored until the finger lifts, so it can't start a chain the moment the level resumes.
- **`PauseController`** (Game) turns `OnApplicationPause(true)` and `OnApplicationFocus(false)` into `LevelPause.Pause()` and reads the back button. Back once the result is shown calls `AndroidApp.MoveToBackground()` (`moveTaskToBack` on the activity; nothing in the Editor).
- **Back button:** a `Back` action in `MyceliaInput`'s Gameplay map, bound to `<Keyboard>/escape`, which is how Android's back button reaches the Input System. Escape does the same in the Editor.
- **Frame rate:** 30 fps under the overlay, back to 60 on resume, from `FrameRatePolicy` (ADR-0010).
- **Overlay:** its own canvas, drawn above every other canvas (sorting order 20). It holds a dim over the level and a panel styled like the result panel. Only its buttons take touches (ADR-0012 rule 2).

### Refinements (2026-10-23, Milestone 2 slice 7)

Decided while building:

- **Focus loss pauses only in a player, not in the Editor**, where focus moves to other windows all the time (and headless runs may report no focus).
- **Audio pause is deferred:** the game has no audio yet. The audio service pauses when it arrives, as another `IPausable`.
- **Saving on pause** (ADR-0006) arrives with the save system in Milestone 3.
- **Guards:** EditMode tests check the pause rules, the input lock, the overlay, the frame rate under pause and the scene entry's wiring. Two project guards were added: the input asset must bind Back to Escape, and the pause canvas must draw above every other canvas (all in Screen Space – Overlay). A PlayMode test checks that a paused move holds still and finishes once resumed.

### Refinements (2026-10-24, Milestone 3 slice 4)

Decided with the main menu and level select (ADR-0012 Refinements):

- **Back goes level select → main menu → background.** Back on level select returns to the main menu; back on the main menu, the app's first screen, sends the app to the background (`AndroidApp.MoveToBackground`). *Why:* Android's convention: back steps out one screen, and leaves from the first one without killing the app.
- **Back on the result panel now opens level select** (the follow-up below). `PauseController` takes what "leave" means from the Game scene's entry, instead of sending the app to the background itself.
- Menus read back through a small `BackButton` component (the same Back action as the Game scene), and their entries decide what it does.

### Refinements (2026-10-24, Milestone 4 slice 1)

- **The background spores pause with the level.** The Game scene's `Backdrop` is an `IPausable` like everything else that animates, so the whole screen holds still under the pause overlay. The menus have no level to pause, so theirs keep drifting.

### Refinements (2026-10-25, Milestone 4 slice 4)

- **A pause button in the HUD** is a fourth way in, beside back, the background and focus loss; it calls the same `LevelPause.Pause`, so it does nothing once the level is over.
- **The overlay offers Resume, Restart and Quit.** The brief asked for Resume and Quit; Restart stays. *Why (Soni):* a bad start can be replayed in one tap, as most puzzle games allow. **Quit is the old Level select**, renamed, and still leads there. *Why (Soni):* level select shows progress and is one step out, like back.
- Seen on the phone: the overlay's dim doesn't take touches (rule 2 of ADR-0012), so the HUD's pause button can still be pressed under it; that pauses nothing more. The settings button (slice 5) will sit beside it, so slice 5 decides what a tap under the overlay may do.

## Consequences

- **Positive:**
  - One place decides when a level can pause, and every part that reacts to a pause gets a single call through `IPausable`.
  - A move can't be committed or keep animating while the player is away, and no input carries over from before the pause.
  - Nothing global changes: no `Time.timeScale`, no static state.
- **Negative:**
  - Anything that animates later (UI tweens, effects, audio) must register as an `IPausable`, or it keeps running under the overlay.
  - Focus-loss behaviour can only be checked on device.
- **Follow-up:**
  - Slice 8, on device: background mid-drag and mid-playback, the notification shade, and back on each screen, including that back on the result panel returns to the same screen when the app is reopened. *Checked on the mid-range phone on 2026-10-23: all as intended.*
  - Milestone 3: save on pause (ADR-0006); back on the result panel goes to level select instead. *Both done: save on pause in slice 3, back to level select in slice 4.*
  - **Revisit if:** something must keep moving under the overlay that can't own its pausing, or the number of `IPausable`s makes the wiring awkward.
