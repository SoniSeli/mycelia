# ADR-0012: Build the game's UI with uGUI and TextMeshPro

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-23 |
| **Deciders** | Soni Seli |
| **Related** | [design §4](../design.md#4-player-flow) (screens), [design §6](../design.md#6-mobile-considerations) (safe area, readable text), [ADR-0008](0008-pointer-action-and-grid-hit-testing.md) (board input), [ADR-0009](0009-animate-playback-with-primetween.md) (PrimeTween, 0 B per move), [performance budget](../performance-budget.md) |

## Context

Milestone 2 slice 6 adds the HUD (moves left, objective counters) and a minimal win/lose panel. Later milestones add the fact card (text, image and credit), level select (12 levels), settings and the intro tutorial (design §4).

Constraints:
- Portrait phones from 16:9 to about 21:9 and tablets, laid out inside the safe area (design §6).
- Readable text at phone size; accessible labels for spores (ADR-0005).
- **0 B of GC per move** includes the HUD updating its counters during playback (ADR-0009). Gameplay must stay within ≤ 30 batches. *Amended 2026-10-24: the budget now limits SetPass calls (≤ 10) and render-thread time instead of batches.*
- One developer: one UI system to learn, style and debug.

Both systems are already available: `com.unity.ugui` 2.0 (which includes TextMeshPro in Unity 6) and the UI Toolkit module, which the project's custom Inspectors use. PrimeTween 1.4.11 can animate either (`RectTransform`/`Graphic` and `VisualElement` tweens).

## Options considered

### Option A: UI Toolkit (UXML, USS, runtime data binding)
- **Pros:** Flexbox layout and shared USS styles suit flat panels, text cards and a level grid. No GameObject per element; changing a label repaints only that element. Same technology as the project's Inspectors. Unity's direction for new UI.
- **Cons:** No built-in safe-area handling. Particles and other world effects over UI need workarounds. Fewer mobile-game examples, and many studio codebases still use uGUI.

### Option B: uGUI with TextMeshPro
- **Pros:** The most widely shipped UI system in mobile games, so its performance pitfalls and their fixes are well known. Animator, particles, masks and PrimeTween work directly on `RectTransform`s and `Graphic`s. Safe-area fitting is a well-known small component. TextMeshPro gives SDF text with allocation-free numeric updates.
- **Cons:** A GameObject per element. A change to any element rebuilds its whole canvas, so canvases must be split by how often they change. Layout groups are expensive. Consistent styling depends on prefab discipline.

### Option C: uGUI for the in-level HUD, UI Toolkit for menus and cards
- **Pros:** Each system where it is strongest.
- **Cons:** Two UI systems, two text pipelines and two sets of patterns to maintain in a one-person project.

## Decision

We will build the game's UI with **uGUI and TextMeshPro (Option B)**.

The deciding factor is **maturity on mobile**: uGUI is what most shipped mobile games and studio codebases use, so its costs are well understood and the rules below keep it within the budget.

### Usage rules (to stay within budget)

1. **Split canvases by how often they change.** Static frames and labels live on one canvas; values that change during play (moves, objective counters) live on their own small canvas, so an update rebuilds only that.
2. **Raycast Target off** on every graphic that isn't interactive, and a `GraphicRaycaster` only on canvases with buttons. The HUD must never block touches meant for the board (ADR-0008).
3. **No layout groups on canvases that change during play.** Menus may use them, since they are laid out once.
4. **Safe area:** a `SafeAreaFitter` on each canvas's root `RectTransform` fits it to `Screen.safeArea` at start, and again whenever the safe area changes (as `BoardCamera` does). The HUD sits in the top band that `BoardLayoutSettings` keeps free (ADR-0011).
5. **Allocation-free text:** counters update with TextMeshPro's numeric `SetText` overloads (for example `SetText("{0}", value)`), never `ToString()` or string concatenation, and only when a value changes.
6. **Animation with PrimeTween** on `RectTransform`s and `Graphic`s, following the ADR-0009 rules. No `Animator` on UI, because it dirties its canvas every frame.
7. **Sprites in a UI atlas**, separate from the board atlas. Fonts are SDF font assets with a static atlas for the shipped glyphs.
8. **Scaling:** one `CanvasScaler` setup for every canvas, *Scale With Screen Size* with a portrait reference resolution. Its values are tuned on device in slice 6.

### Refinements (2026-10-23, Milestone 2 slice 6)

Decided while building the HUD:

- **Counter timing:** moves left drop the moment a chain is released. Goal counters update, with a short pulse, as the cleared spores finish popping, at the end of the Clear phase. The result panel opens once the whole move has played. `MovePlayback` reports all three through `IPlaybackListener`, which `HudView` implements.
- **Goal display:** the spore's silhouette in its colour, then `collected/target` (for example `7/12`).
- **Result panel:** "Level complete" with three stars, earned ones lit, or "Out of moves" without stars. **Play again** reloads the level through the `SceneLoader` with a fresh seed. Level select and the fact card come in Milestone 3.
- **Font:** LiberationSans SDF from TextMeshPro's essential resources (SIL OFL) is a placeholder until the art pass. TMP's sample emoji set is removed and emoji support is off: the game uses none, and its licence terms are unclear.
- **Scaling (rule 8):** reference resolution 1080 × 1920 in *Expand* mode, so the reference area always fits: tall phones gain height and tablets gain width. The HUD fills the top band of the safe area that `BoardLayoutSettings` keeps free.
- **Canvases:** a HUD canvas (frame and caption) with a nested canvas for the counters, and a separate result canvas, the only one with a `GraphicRaycaster`. Each has one safe-area root.
- **Rule 5 in the Editor:** TextMeshPro's numeric `SetText` allocates *only in the Editor*, where it rebuilds the string for the Inspector (`#if UNITY_EDITOR` in `TMP_Text.SetText`). Players don't allocate, so the 0 B check for the counters is part of the device pass (slice 8). The EditMode test guards our own part, the goal pulse.
- **Guards:** EditMode tests check the real Game scene against rules 1–4, 7 and 8: only buttons take touches, counters sit on their own canvas with no layout group, every canvas scales alike and has one safe-area root, every UI image draws from the UI atlas, and there is one EventSystem using the Input System UI module.

### Refinements (2026-10-24, Milestone 3 slice 4)

Decided while building the main menu and level select:

- **Launch opens a main menu first** (title and Play; Settings joins it in Milestone 4), then level select. *Why:* a title screen gives the game a front door with its name and art, which matters for a portfolio piece people open once to judge. This settles the open question in design §4/§9. The title is placeholder text in the placeholder font until the art pass.
- **Each screen is its own scene** (`MainMenu`, `LevelSelect`) with its own entry, like `Game` (ADR-0010). *Why:* each screen keeps one entry and its own back rule, and the scene guards run on each. Switching costs a short scene load at the 30 fps menu rate.
- **Level select is a fixed 3 × 4 grid**, all levels on one portrait screen with no scrolling; the buttons are made once when the screen opens and placed by a `GridLayoutGroup` (allowed by rule 3: the menu canvas never changes during play). *Why:* 12 levels fit one screen, so scrolling would add work and nothing for the player.
- **A locked level shows a lock and doesn't react to taps.** *Why:* the player sees how many levels there are and that winning opens the next.
- **Each unlocked level shows its number and best stars** (three, earned ones lit). *Why:* stars are the replay hook (design §3.4), so showing them here gives a reason to replay.
- Smaller choices: level buttons are 280 × 280 with 50 between them, in the button green, with a white lock icon (own placeholder art, in the UI atlas); a locked button is dimmed by the button's own disabled tint; the main menu's subtitle reads "A fungal chain puzzle".
- **Guards widened:** the "only buttons take touches", canvas scaling and safe-area, UI atlas and EventSystem checks now run on every scene the player sees, not only Game. A new guard checks that level select lays levels out three across with no scroll view.

### Refinements (2026-10-24, Milestone 3 slice 5)

Decided while building the level flow (design §4):

- **A win returns to level select, with no Next level button.** *Why:* coming back to the grid lets the player see the new unlock and their stars after every win, which makes progress feel real.
- **The fact card sits on the result panel**, under the stars: the level's image if it has one, the text, and the image credit. *Why:* one screen, nothing to tap through.
- **The fact shows on every win**, not only the first. *Why:* simple, with nothing extra to save; replays are short.
- **A loss offers Retry and Level select.** *Why:* retrying is the usual next step after a loss, one tap away, and level select is the way out (design §4).
- **The pause overlay adds Level select** (Resume, Restart, Level select). *Why:* the player can leave a level without finishing it; a level in progress isn't saved anyway (ADR-0006).
- **The card's height follows the outcome**: short for a loss, taller for a win, taller again when the fact has an image. The title and stars are anchored to the card's top and the buttons to its bottom; `ResultPanel` sets the heights and moves the fact text below an image. No layout group (rule 3).
- **Fact images are content, not UI art:** the scene's fact image holds a UI atlas sprite, and at runtime it shows the level's own image, outside the atlas (rule 7 covers the screens' own art). That costs one extra draw on a screen shown once per level.
- *Refined 2026-10-24 (slice 7):* **the fact text is shown in full and the card grows to fit it.** The text box takes the height its text needs (at least its scene height), the credit moves below it, and the card grows by as much; the fact text is 36 pt. *Why (Soni):* the detail is the point; cutting a fact to one sentence loses the explanation that makes it interesting. **The image credit shows only with its image**, so a build without the local-only photos ([facts.md](../facts.md)) shows neither.
- Smaller choices: leaving a level (the result panel, the pause overlay, or back) writes anything unsaved first, which covers a win whose move was still playing when the player paused and left; Retry and Level select are both in the button green on the result panel, and Level select is grey (secondary) on the pause panel; the fact text is top-aligned, 42 pt, with the credit in 28 pt caption grey.

## Consequences

- **Positive:**
  - Familiar, well-documented technology for the HUD, panels and their effects.
  - The rules turn uGUI's known costs (canvas rebuilds, raycasts, layout) into checks code review and tests can catch.
- **Negative:**
  - More GameObjects and prefab discipline for consistent styling.
  - Text-heavy screens (fact cards, settings) need care with layout groups, which UI Toolkit would have made easier.
  - The project now uses UI Toolkit for Editor tooling and uGUI for the game; that split is intentional.
- **Follow-up:**
  - Slice 6: import TextMeshPro's essential resources, add `SafeAreaFitter` with EditMode tests, and build the HUD and win/lose panel driven by playback's `ObjectiveProgressed` and `SessionEnded` steps (ADR-0009). Board input must be locked while a panel is open.  *Done in slice 6. Board input is already locked once a level ends (`ChainDrag`); a pause overlay (slice 7) will need its own lock.* *Done in slice 7: `ChainDrag` also locks while `LevelPause` is paused, and the pause canvas draws above the others ([ADR-0013](0013-pause-the-level-and-handle-back.md)).*
  - Slice 8: measure canvas rebuilds, batches and GC with the HUD updating during a move, on device. *Measured on the mid-range phone with `PerfProbe` during real moves with the real HUD: mostly 0 B of GC per frame after warm-up, but small allocations (24 B to 1.3 KB) in some windows. *A Profiler capture (2026-10-24) found none in the HUD's counter updates, which settles rule 5 in a player; uGUI's EventSystem allocates once on each level's first touch (first-use, counted as warm-up).* Batches are no longer the budget's measure ([performance budget](../performance-budget.md)). Canvas rebuilds weren't measured separately; they're part of the main-thread time.*
  - Choose and credit a font (design §10).
  - **Revisit if:** the UI grows into data-heavy screens (long lists, rich text) where uGUI's costs show up in profiling, or Unity stops developing uGUI.
