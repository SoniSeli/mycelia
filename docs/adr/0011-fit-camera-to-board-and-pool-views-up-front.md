# ADR-0011: Fit the camera to the board, pool spore views up front, and pack board sprites in one atlas

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-23 |
| **Deciders** | Soni Seli |
| **Related** | [design §6](../design.md#6-mobile-considerations) (portrait, 16:9 to ~21:9, tablets, safe area), [ADR-0002](0002-use-urp-with-2d-renderer.md) (unlit sprites), [ADR-0008](0008-pointer-action-and-grid-hit-testing.md) (grid hit testing), [performance budget](../performance-budget.md) (0 B per move, ≤ 30 batches) |

## Context

Milestone 2 slice 3 draws the starting board. Four questions came up together:

- **Fitting:** the board must fill its space on portrait phones from 16:9 to about 21:9 and on tablets, inside the safe area (notches, gesture bars), and leave room for the HUD that slice 6 adds.
- **HUD space:** the HUD's framework isn't chosen yet, but the board must not have to move when it arrives.
- **Pooling:** the budget allows no GC allocation during a move, and the done-when for this slice is "no `Instantiate`/`Destroy` during play". Spores are cleared and spawned on every move.
- **Atlas:** every spore silhouette and the cell tile should draw from one texture, so the board batches well (≤ 30 batches).

## Options considered

### Fitting
- **A: Camera fits board.** The board stays in world units (one cell = one unit, matching the 256 px-per-unit sprites); the orthographic camera's size and position are computed. *Pros:* world positions are cell positions, so hit testing (ADR-0008) needs no scale; the maths is pure and testable. *Cons:* anything else in the world (backgrounds, effects) moves with the camera.
- **B: Fixed camera, scale the board.** *Pros:* fixed backgrounds. *Cons:* every world position goes through the board's scale.

### HUD space
- **A: Tunable insets asset.** A ScriptableObject with a top band, a bottom band and side margins as fractions of the safe area. *Pros:* tuned on device without code changes; the board is already in its final place. *Cons:* the numbers are guesses until the HUD exists.
- **B: Fill the safe area for now.** *Pros:* nothing to tune yet. *Cons:* the layout changes again in slice 6.

### Pooling
- **A: Prewarm twice the cells, fixed.** *Pros:* room for new spores while cleared ones animate out; running out is a visible bug, never a hidden `Instantiate`. *Cons:* a little memory for views that may never be used (72 for a 6 × 6 board).
- **B: Prewarm one per cell, grow on demand.** *Pros:* can't run out. *Cons:* growth quietly breaks the no-`Instantiate` rule and allocates mid-move.

### Atlas
- **A: A committed SpriteAtlas (v2) asset** over the spore and board art folders. *Pros:* simple, visible in the Inspector, guarded by a test. *Cons:* new art folders must be added to it.
- **B: Generate the atlas at build time.** *Pros:* nothing to forget. *Cons:* more machinery, harder to inspect.

## Decision

We will use **option A for all four**: the camera fits the board, insets live in a `BoardLayoutSettings` asset, the spore view pool is prewarmed to twice the cells and never grows, and board sprites are packed in one committed atlas.

The deciding factor is **keeping the board in cell units and keeping every cost at load time**. Pure layout maths serves rendering, input and tests alike, and nothing is created or destroyed once play starts.

### Shape

- **`BoardLayout.Fit(columns, rows, screenSize, safeArea, insets)`** (pure struct): the board area is the safe area minus the insets (top 20 %, bottom 8 %, sides 4 % to start). Pixels per cell is the smaller of area width ÷ columns and area height ÷ rows, so cells stay square and the tighter direction is filled. The board's centre goes to the area's centre. It exposes `OrthographicSize`, `CameraPosition`, `WorldToScreen` and `ScreenToWorld`. The board's origin is the world origin, at its bottom-left corner.
- **`BoardCamera`** applies the layout to the camera and refits when the screen size or safe area changes.
- **`SporeViewPool`** creates all its views in its constructor. `Rent` throws when empty; returning a view twice throws.
- **`BoardView.Init(board, catalog)`** creates the pool, a tile under every playable cell, and a spore view per spore, all before play. Spores draw above tiles (sorting order 1 over 0). The board object must sit at the world origin.
- **Atlas:** `Art/BoardAtlas.spriteatlasv2` packs `Art/Spores` and `Art/Board`: padding 4, no rotation or tight packing, no mipmaps, ASTC 4×4 on Android.
- **Materials:** both prefabs use URP's `Sprite-Unlit-Default` (ADR-0002).

## Consequences

- **Positive:**
  - Layout rules are EditMode tests on six screens (16:9 to 21:9 phones with notches, 4:3 and 16:10 tablets) and three board shapes.
  - The input slice can map touches with `BoardLayout.ScreenToWorld` and integer maths.
  - Guards: one atlas packs every catalog sprite and the cell tile; prefabs use the unlit material; every Mycelia component in every scene has its references set; the board sits at the world origin.
- **Negative:**
  - On tall phones the board is centred in a tall area, which leaves empty space above and below it until the HUD and background fill it. The insets are the dial.
  - New art folders must be added to the atlas (the atlas test catches spores, not other sprites).
- **Follow-up:**
  - Slice 6: tune the insets around the real HUD.
  - Slice 8: measure batches against the ≤ 30 budget on device.
  - **Revisit if:** levels need boards too large for one screen (scrolling), or the board needs a fixed background independent of the camera.

## Evidence

Renders of the starting board on all six screens, with the unsafe area and HUD bands tinted, showed the board filling its area and clear of both (2026-10-23, local render script).
