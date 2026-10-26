# ADR-0016: Drive Android haptics through the vibrator's effects, behind a haptics interface

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-26 |
| **Deciders** | Soni Seli |

## Context

Milestone 4 slice 9 (design §8.3) asks for a light tick per spore added to a chain and a stronger one on commit and on a win, following the saved haptics switch (ADR-0006, settings modal from slice 5), at 0 B per move (ADR-0009). The game targets Android 8+ (API 26). The mid-range test phone runs Android 13 and reports amplitude control and Android's built-in effects (TICK, CLICK, DOUBLE_CLICK, HEAVY_CLICK and more) in `dumpsys vibrator_manager`, but no composition primitives.

## Options considered

### Option A: The vibrator's effects, called through JNI
- **Pros:** Each moment gets its own strength: Android's built-in effects on Android 10+, one-shots with an amplitude below that. Everything Java can be looked up once at startup, so a call is one JNI call with arguments made in advance. No Java source in the repository.
- **Cons:** Needs the VIBRATE permission, which Unity adds on its own only for `Handheld.Vibrate`. The phone's own "touch feedback" setting doesn't silence it; only the game's switch does.

### Option B: `View.performHapticFeedback`
- **Pros:** No permission; follows the phone's touch-feedback setting as well as the game's.
- **Cons:** Must run on Android's UI thread, not Unity's, so it needs a Java helper class built by Gradle, and it lands about a frame late. The phone's setting can silence the game even with the game's switch on.

### Option C: `Handheld.Vibrate`
- **Pros:** One line, no JNI, Unity adds the permission.
- **Cons:** One fixed buzz of about 400 ms with no strengths: no ticks, so it could only mark a win.

## Decision

We will play haptics through an **`IHaptics` interface** (`Mycelia.Game`, one `Play(Haptic)` call for the moments `Tick`, `Commit`, `Win` and `Loss`), implemented on Android by **`AndroidHaptics`** (Option A). **Deciding factor (Soni):** ticks need their own strengths, which only the vibrator's effects give; `Handheld.Vibrate` has no ticks, and View feedback needs Java code and lets the phone's switch silence ours.

### Details

- **What is felt** (`ChainHaptics` sits between chain input and the chain view and between chain input and the move player, `OutcomeHaptics` in front of the win wave):
  - A spore joins the chain, or is dragged back off it: **Tick**. A touch on a spore that can't join is silent. *Why (Soni):* every change in the chain's length should be felt.
  - The chain is committed: **Commit**, as the move is handed to playback.
  - The level is won: **Win**, as the win wave starts. Lost: **Loss**, as the loss card shows. *Why the loss (Soni; advised against):* a soft pulse marks the loss without punishing it, and the game feels unfinished when only good news is felt.
  - Nothing in the intro's chain demo, and no button taps.
- **Strengths, felt on the phone** (`AndroidHaptics.For`): Tick = the built-in TICK, Commit = CLICK, Win = one 150 ms buzz at full strength, Loss = an 80 ms pulse at 27 % (amplitude 70 of 255). Below Android 10, Tick and Commit fall back to 10 ms at 25 % and 20 ms at 59 %. The first try, HEAVY_CLICK for the win, felt too close to the commit; four candidates (DOUBLE_CLICK, a triple pulse, the long buzz, a rising ramp) were played after a CLICK from adb (`cmd vibrator_manager synced …`). *Why the win (Soni):* felt side by side, a long full buzz stood apart from the commit best. The loss kept its first strength against a firmer and a longer one.
- **The switch:** `AppServices.Haptics` is a `SwitchedHaptics`, which reads `PlayerProgress.Settings.Haptics` on every call, so a change in the settings modal applies at once. Switching haptics on plays one Tick (`SettingsSwitches.Choose`). Scenes never see the device's haptics directly.
- **No pause hook** (an exception to ADR-0013's "everything that animates registers as an `IPausable`"): every haptic is a one-shot of at most 150 ms started by a touch, a commit or an outcome, and none of those can happen while the level is paused. *Why (Soni):* every haptic is over before a pause can matter, and the tick shows that the switch works.
- **Allocation-free:** `AndroidHaptics.Create` (at boot, in `AppRoot`) looks up the vibrator, its `vibrate(VibrationEffect)` method and one `VibrationEffect` per moment, each with its `jvalue[]` arguments made in advance; `Play` is then one `AndroidJNI.CallVoidMethod` and an exception check. It never throws: if anything fails, it logs a warning and goes quiet (`NoHaptics` in the Editor and on phones without a vibrator).
- **Permission:** `AndroidVibratePermission` (an `IPostGenerateGradleAndroidProject` step in `Mycelia.App.Editor`) adds `android.permission.VIBRATE` to the generated manifest. VIBRATE is a normal permission, granted at install without a prompt. The step only compiles where Android build support is installed (`#if UNITY_ANDROID`), because the CI test runner has none.

### Measured on the phone (2026-10-26, development build without assertions)

- `dumpsys vibrator_manager` lists the game's TICKs and CLICKs as `finished`. With the saved switch off (Soni's save had it off), nothing was played.
- Playing level 5 and level 1 (about 80 s, haptics on): the play windows showed 0 B GC except one window of 1,480 B in 4 frames during level 5, after many moves (not traced). The first moves of the new run allocated 4.9 KB (the known first-moves warm-up), and the level end 430 KB (the known level-end cost on the watch list). SetPass max 9, as before.

## Consequences

- **Positive:**
  - Game code only knows "a tick happened"; strengths live in one table, and the switch in one place.
  - Tested without a phone: chain, commit, outcome and switch behaviour run in EditMode against a fake.
- **Negative:**
  - JNI code is only compiled in Android builds and only checked on the phone.
  - The phone's own touch-feedback setting doesn't affect the game.
- **Follow-up:**
  - The low-end reference device (Milestone 5): check the Android 8–9 fallback strengths and a phone without amplitude control.
  - Slice 10 (audio) can add sound to the same moments next to these. *Done: `ChainSounds` and `OutcomeSounds` sit in front of these ([ADR-0017](0017-play-sound-with-playoneshot-behind-a-sound-interface.md)).*
  - **Revisit if:** a longer pattern (one that could outlast a pause) is wanted, or players ask for haptics to follow the phone's touch-feedback setting.
