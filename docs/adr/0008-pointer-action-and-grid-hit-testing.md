# ADR-0008: Read input through one pointer action and map it to cells with grid maths

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-22 |
| **Deciders** | Soni Seli |
| **Related** | [ADR-0003](0003-separate-pure-domain-from-views.md) (domain owns the drag rules via `Chain.Touch`), [design §3.2, §6](../design.md) |

## Context

The player drags one finger across the board. The domain already decides what a touch means: `Chain.Touch(cell)` starts, extends, backtracks or rejects. The view only has to answer **"which cell is the finger in right now?"** and feed that cell to the chain.

Requirements:
- One finger. A second finger must not disturb the chain (design §6).
- **Diagonal drags** must not accidentally catch the two side neighbours they pass between. Design §6 says a spore joins only when the finger is within a **circular radius** of about 0.4 of a cell from its centre.
- Fast swipes must not skip cells between frames.
- The mouse must work in the Editor, and touch must work on device.
- The project is set to **Input System only** (Milestone 0).
- No per-frame allocations (performance budget).

## Options considered

### Option A: One Input System action bound to `<Pointer>`, plus grid maths
- **Pros:** `<Pointer>` follows the **primary touch** on a phone and the **mouse** in the Editor, so one code path covers both and extra fingers are ignored. Hit-testing is a few lines of maths that are easy to test, with the exact circular radius the design asks for. No colliders, no physics.
- **Cons:** Interpolating fast swipes and the radius rule are our own code to write and test.

### Option B: EnhancedTouch API (`Touch.activeTouches`)
- **Pros:** Full multi-touch detail (finger ids, phases).
- **Cons:** More code for a one-finger game, plus a separate mouse path for the Editor.

### Option C: uGUI pointer events on per-spore colliders (Physics2DRaycaster)
- **Pros:** Familiar event callbacks.
- **Cons:** 36 colliders and physics raycasts every frame. Hit areas follow collider shapes rather than the design's radius rule, and diagonal drags would need special-casing anyway.

## Decision

We will use **Option A**: a small input action map with **`Point` (`<Pointer>/position`)** and **`Press` (`<Pointer>/press`)**, and a `ChainInputController` that converts the pointer position to a cell with **grid maths** and passes it to `session.Chain.Touch`.

The deciding factor is that **the design's hit rule is geometric, not physical**: a circle around each cell centre is simplest and most precisely expressed as maths, and `<Pointer>` gives one-finger, Editor-friendly input with no extra code.

### Details

- **Action asset:** a dedicated `MyceliaInput.inputactions` with one `Gameplay` map (`Point`, `Press`). The template's general-purpose `InputSystem_Actions` asset is removed in Milestone 2.
- **Mapping** (pure C#, unit-tested in EditMode):
  1. screen → world through the board camera;
  2. world → nearest cell through the board layout (origin, cell size);
  3. **accept the cell only if the point is within `HitRadius` (0.4 × cell size) of its centre**, otherwise report "no cell". The finger sitting in a corner between cells does nothing.
- **Fast swipes:** between the last and current pointer positions, the controller samples points every half cell along the segment and touches each accepted cell in order, so a quick swipe can't skip a spore.
- **Flow:** press → start sampling; release → if `Chain.IsCommittable`, call `session.Commit()` and hand the result to playback, otherwise clear the chain. Input is **ignored while playback runs** and once the session has ended.
- **Feedback:** the controller raises the `ChainChange` from each touch so the line view, sound and haptics can react (e.g. a light tick on `Rejected`).
- **Allocations:** positions are value types; reading actions with `ReadValue<Vector2>()` and `IsPressed()` does not allocate.

### Refinements (2026-10-23, Milestone 2 slice 4)

Decided while building touch input:

- **Actions arrive as `InputActionReference`s** to `Gameplay/Point` and `Gameplay/Press` in `MyceliaInput.inputactions`, serialized on `ChainInputController`, which enables them. No generated C# class, and **no project-wide actions**: the template's `InputSystem_Actions` asset and its project-wide registration are removed, since `InputSystem.actions` is global state (ADR-0010).
- **Tuning lives in a `ChainInputSettings` asset:** `HitRadius` 0.4 and the swipe sample step 0.5, in cells.
- **The logic is in `ChainDrag`**, which takes press, move and release in board coordinates (from `BoardLayout.ScreenToWorld`). It is tested directly, including a 0 B allocation test for dragging. `ChainInputController` only reads the actions and forwards them. A drag that starts between cells begins at the first spore it reaches.
- **Feedback goes through `IChainFeedback`:** every touched cell, including `Rejected` ones (for later sound and haptics), and the end of the drag. `ChainView` draws a line through the chained spores' centres, above the spores, and draws the chained spores at 1.15× size, so the chain reads without colour.
- **Until move playback exists (slice 5), a committed move snaps the board:** `BoardView.Snap` redraws every spore from the new board using pooled views. `MovePlayback` replaces it. *Replaced in slice 5: `ChainDrag` now hands committed moves to an `IMovePlayer` (ADR-0009 refinements).*

### Refinements (2026-10-23, first run on device)

Found on the mid-range phone (Redmi Note 12 Pro+ 5G), CI build of `afd1c41`:

- **`HitRadius` goes from 0.4 to 0.45.** The deciding factor: at 0.4, catching a spore felt slightly too tight on the phone. At 0.45, a drag straight along a diagonal still passes at least 0.26 of a cell from each side neighbour's circle (0.31 before). The `CellHitTest` tests for diagonals now also run at 0.5, the largest radius `ChainDrag` accepts, so every value the setting allows keeps diagonal drags off the side neighbours.
- **A first finger lifting while a second is down** does not move the chain to the second finger, so no extra handling is needed.

## Consequences

- **Positive:**
  - One code path for phone and Editor; a second finger is ignored by construction.
  - The diagonal-drag problem is solved by an explicit, tunable number (`HitRadius`) with tests, not by collider tweaking.
  - No physics in the input path.
- **Negative:**
  - The swipe interpolation and the radius rule are our own code to maintain.
  - **To verify on device:** what `<Pointer>` does when the first finger lifts while a second is still down. The chain must not jump to the second finger mid-drag; if it does, the controller cancels the drag on the primary touch's release. *Checked on the mid-range phone (2026-10-23): it doesn't jump.*
- **Follow-up:**
  - Milestone 2: `MyceliaInput` asset, `BoardLayout` and `CellHitTest` with EditMode tests (centre, edge of radius, corner gaps, diagonal swipe sampling), and `ChainInputController`. *Done in slice 4 (`BoardLayout` in slice 3); a headless Play check drove a virtual mouse through a chain and committed the move. Tried on the mid-range phone on 2026-10-23.*
  - Tune `HitRadius` on the mid-range and low-end devices. *0.45 on the mid-range phone (refinements above); the low-end device is still to check.*
  - **Revisit if:** a mechanic needs gestures (pinch, two-finger actions). EnhancedTouch (Option B) can then be added beside the pointer action.
