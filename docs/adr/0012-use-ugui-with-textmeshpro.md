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
- **0 B of GC per move** includes the HUD updating its counters during playback (ADR-0009). Gameplay must stay within ≤ 30 batches.
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

## Consequences

- **Positive:**
  - Familiar, well-documented technology for the HUD, panels and their effects.
  - The rules turn uGUI's known costs (canvas rebuilds, raycasts, layout) into checks code review and tests can catch.
- **Negative:**
  - More GameObjects and prefab discipline for consistent styling.
  - Text-heavy screens (fact cards, settings) need care with layout groups, which UI Toolkit would have made easier.
  - The project now uses UI Toolkit for Editor tooling and uGUI for the game; that split is intentional.
- **Follow-up:**
  - Slice 6: import TextMeshPro's essential resources, add `SafeAreaFitter` with EditMode tests, and build the HUD and win/lose panel driven by playback's `ObjectiveProgressed` and `SessionEnded` steps (ADR-0009). Board input must be locked while a panel is open.
  - Slice 8: measure canvas rebuilds, batches and GC with the HUD updating during a move, on device.
  - Choose and credit a font (design §10).
  - **Revisit if:** the UI grows into data-heavy screens (long lists, rich text) where uGUI's costs show up in profiling, or Unity stops developing uGUI.
