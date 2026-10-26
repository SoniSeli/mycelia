# ADR-0017: Play sound with PlayOneShot on preloaded clips, behind a sound-effects interface

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-26 |
| **Deciders** | Soni Seli |

## Context

Milestone 4 slice 10 (design §8.3) asks for sound effects and music at the saved volumes (ADR-0006: a sound is on while its volume is above 0, the settings modal's switches from slice 5), pausing with the level and in the background (ADR-0013), at 0 B per move (ADR-0009). The repository had no sound files, and nothing third-party may be committed unless its licence allows it (design §10). Slice 9 (ADR-0016) put haptics behind an `IHaptics` interface with hooks for the chain and the outcome; sound can use the same moments. Before this slice the only `AudioListener`s were on the Bootstrap and Game cameras, so the menus, which load after Bootstrap is unloaded, had none.

## Options considered

### Where the sounds come from
- **Made by code (chosen):** a local Editor script synthesises every sound into WAV files. Own work, committed, so every build (CI included) has sound. Placeholder quality.
- **CC0 packs (e.g. Kenney):** better sounding and committable with their licence file, but downloaded third-party content.
- **Soni's own recordings:** the slice would wait for them.
- **Asset Store, local only:** kept out of git like the fact photos; CI and clean checkouts would be silent, and the licence would block a release.

### How effects play (both measured on the phone, below)

#### Option A: `AudioSource.PlayOneShot` on clips loaded up front
- **Pros:** Sounds overlap freely on one source; one `Stop()` silences everything a source played; the volume is passed per call. Least code.
- **Cons:** No per-sound control (pitch, stopping one sound).

#### Option B: A pool of 8 `AudioSource`s, clip + `Play()`
- **Pros:** Per-sound pitch and stop.
- **Cons:** A fixed number of voices that cut off the oldest, and the round-robin bookkeeping to keep allocation-free.

## Decision

We will play effects through an **`ISoundEffects` interface** (`Mycelia.Game`: `Play(SoundEffect)` for `Commit`, `Win`, `Loss` and `Tap`, `PlayTick(chainLength)` for the chain, and `IPausable`), implemented by **`PlayOneShot` on clips decompressed at load** (Option A), with sounds **made by code**. **Deciding factors (Soni):** both routes measured 0 B per call on the phone at the same cost, so the simpler one wins; the rising ticks are separate clips, so no per-voice pitch is needed. Sounds made by code are our own work and can be committed, like the spores and the UI kit drawn by code, so every build has sound; they can be replaced in Milestone 5.

### Details

- **What is heard** (`ChainSounds` and `OutcomeSounds` sit in front of `ChainHaptics` and `OutcomeHaptics`):
  - A spore joins the chain or is dragged back off it: **a tick, one step of a C major pentatonic scale per spore** (D4 to E6, 12 steps; longer chains stay on the top step), so a growing chain climbs and a shrinking one steps back down. A touch on a spore that can't join is silent. *Why (Soni):* the chain's length is heard as well as felt.
  - The chain is committed: **a soft double bubble pop** (two small rising bubbles 60 ms apart). The level is won: **a rising chime** as the win wave starts; lost: **two soft falling tones** as the loss card shows.
  - **Every button taps** (`ButtonTaps.Attach`, called last in each scene entry's `Construct`, hidden buttons and the settings modal's included), and every settings switch flip taps through the effects switch as it is after the flip: switching effects on is heard, off is silent. Nothing for the intro's looping demo chain. *Why (Soni):* the same moments as haptics plus taps; the demo repeats every few seconds while the player reads.
  - Soni listened to the first versions and asked for softer ticks and a softer bubble pop, then (after playing) ticks and pop about 3 dB quieter and a softer tap, chosen from four candidates: the **"felt" tap**, a low padded thump (260 Hz with a short wooden overtone). The pop went through four candidates and an imitation of a reference sound Soni found (made by code from its measured pitch and envelope; the reference itself isn't in the repository) before Soni chose the double bubble, made softer and about 4 semitones deeper.
- **Music: one loop for every screen**, 32 s (four slow chords and occasional bell notes, rendered as a ring so it loops without a seam), owned by the `AppRoot` so it plays on through scene fades without restarting. **It keeps playing while a level is paused.** *Why (Soni):* one streamed clip is the cheapest, and music is ambience shared with the menus, so the pause screen isn't silent and the music switch is heard when flipped in settings.
- **The switches:** `AppServices.Sounds` is a `SwitchedSoundEffects`, which reads `SfxVolume` on every call and plays at that volume only while it is above 0. `Music.Follow()` runs every frame from `AppRoot.Update`, acts only when `MusicVolume` changed (0 B), plays the loop at that volume, and pauses it at 0 (switching back on carries on where it paused). Both drive an `IAudioOutput`, so EditMode tests record what reaches the device.
- **Pausing** (ADR-0013 Refinements): `ISoundEffects` is an `IPausable` in the level's `LevelPause`; pausing **stops** the level's effects still sounding (none lasts over 2.2 s, and none can start while paused) rather than resuming them, so no paused state can leak into the next scene. Button taps play on their own source, so the pause overlay's and settings' buttons are heard. In the background Unity stops all audio output itself.
- **Device side:** `AudioPlayer` on a child of the `AppRoot`, beside the app's **one `AudioListener`** (moved there from the Bootstrap and Game cameras): a level-effects source, a taps source, and a music source (loop, priority 0). All 2D. No `AudioMixer`.
- **Files and import:** `Assets/_Mycelia/Audio/` (12 ticks, pop, tap, win, loss, music loop; 44.1 kHz mono WAV, 3.4 MB in git, 2.8 MB of it the music). Effects: mono, ADPCM, decompressed on load and preloaded. Music: mono, Vorbis quality 0.5, streamed. The generator is `LocalTools/Editor/MakeSounds.cs` (local, like `DrawSpores`).

### Measured on the phone (2026-10-26, development build without assertions)

- **The two routes**, from a temporary benchmark on the main menu (300 frames per phase, 1 or 5 calls per frame, PerfProbe off): both **0 B** per call after warm-up. `PlayOneShot` allocated 154 B once, on the very first call (warm-up). Mean cost per call 85–191 µs for `PlayOneShot` and 56–167 µs for the pool, maximum 0.4–1.6 ms for both, within each other's noise.
- **In the game** (Soni playing levels 3, 5 and 4 for about 2 minutes, with pause, settings flips, a win and backgrounding): everything heard as intended; effects silent with their switch off, music paused with its switch off and carrying on when switched back on, music playing under the pause overlay, silence in the background (`dumpsys audio`: the game's stream `started` on the menu and in a level, `stopped` in the background). **0 B GC in every window of play** after the known first-moves warm-up (4.9 KB); level end 328 KB and opening pause or settings 39–110 KB, as before (not moves). SetPass 8–9 playing and 10 with settings open, as before; a launch window on the menus read 12 (sound draws nothing; on the watch list).

## Consequences

- **Positive:**
  - Game code knows moments, not clips or volumes; volumes and clips live in one place each, behind fakes in tests.
  - The music survives scene loads and fades with no extra work in the scenes.
- **Negative:**
  - The sounds are placeholders made by code.
  - `PlayOneShot` gives no per-sound control: a per-voice effect (pitch bend, stopping one sound) would need Option B.
  - 2.8 MB of WAV for the music in plain git (the Git LFS question in design §9 is still open).
- **Follow-up:**
  - Milestone 5: memory of the clips in a release build (estimated under 1 MB: about 8 s of effects decompressed, the music streamed) and the APK size change; sound on the low-end device.
  - **Revisit if:** players ask for volume sliders (the save already keeps volumes, ADR-0006), a sound needs per-voice control, or real recordings replace the code-made ones.
