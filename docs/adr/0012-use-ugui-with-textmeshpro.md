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
- **Font:** LiberationSans SDF from TextMeshPro's essential resources (SIL OFL) is a placeholder until the art pass (*replaced by Shantell Sans in Milestone 4 slice 1, and deleted in Milestone 5 slice 1: it sat in a `Resources` folder, so it shipped unused; performance budget Changes 2026-10-27*). TMP's sample emoji set is removed and emoji support is off: the game uses none, and its licence terms are unclear.
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

### Refinements (2026-10-24, Milestone 4 slice 1)

Decided with Soni's UI/UX brief (design §8.3):

- **The game font is Shantell Sans** (Shantell Martin with Arrow Type, SIL Open Font License 1.1), the only font on every text: Bold for titles, buttons and numbers, SemiBold for long text (the fact, its credit, the subtitle). The brief asked for "Paper Noise", which is free for personal use only. Three rounds of rendered previews compared Mali, Sniglet, Delius, Playpen Sans and Shantell Sans. *Why (Soni):* it has the most personality. **Letter spacing −2** (TextMeshPro's normal spacing offset, on both font assets). *Why (Soni):* keep the font's design: a small nudge closer, not a re-cut.
- **Both font assets are static**: every character the scenes, prefabs and level texts use (107, with typographic quotes and dashes) is baked into a 1024 × 1024 atlas, so no glyphs are made while playing, and the font files stay out of the build. A fact using a new character needs the atlas rebuilt; `ProjectFontTests` fails until it is. TextMeshPro's default font for new texts is the SemiBold asset.
- **Every screen has the same backdrop**: the deep blue to turquoise gradient and slowly drifting spores, drawn in the world behind the board and the UI, not on a canvas. A `Backdrop` sits under each scene's camera and refits when the camera changes size (the board camera fits itself to each level); the spores' size, speed and spread scale with the view. The gradient is blended in sRGB, like the reference image, rather than in the project's linear light. *Why world space:* a background canvas would have to bleed outside the safe area and draw behind the board, against rules 4 and 8; two renderers behind everything need neither.
- **The UI kit is drawn for the project** (round icon buttons, modal panel, goal pill, toggle, softer star) as white shapes the UI tints, packed in the UI atlas with Soni's mushroom and level-node shape; later slices put them on the screens.

### Refinements (2026-10-25, Milestone 4 slice 4)

Decided with the gameplay HUD (design §8.3; the brief: goals grouped with moves left at the top, settings and pause buttons, a pause modal with Resume and Quit):

- **One card in the top band with moves left and the goals, and a round pause button centred below the board.** Compared with everything in one bar (four goals get tight next to two buttons) and the mockup's "Objective" pill with moves in the level-node shape; Soni chose the card with the pause button in the top-left corner, then, after seeing it, moved the button below the board. *Why (Soni):* everything the level asks of you reads in one place. `HudView` centres the button in the band the board leaves free below it, from the same insets as the top band.
- **A tab above the card names the level: "Level N"**, from Soni's reference image: the card's colour and the kit's modal shape, rising from the card's top edge. N is the level's place in the catalog, the number level select shows; the Editor's debug level shows "Level". The tab overlaps the card to hide its own bottom corners, so both are now opaque (overlapping see-through panels would show a darker band); the title sits on the counters canvas with the other HUD text, so it adds no draw.
- **The settings button comes with the settings modal in slice 5.** *Why (Soni):* no button that does nothing.
- **Goals share their part of the card evenly** (anchors, not positions), each a spore icon above its count, so up to four fit on the narrowest screen; a guard checks a goal's icon and count stay apart and "40/40" fits the smallest share (170 × 190 units, a 16:9 phone with a notch).
- **Every HUD text sits on the counters canvas.** With the caption on the HUD's own canvas, the same font drew twice: level 11 (four goals) measured SetPass 9 playing and 11 paused on the phone; with it on the counters canvas, 8 and 10, within budget ([performance budget](../performance-budget.md)). The caption never changes, so it costs nothing when the counters rebuild. Rule 1 still holds for what changes.
- **The HUD canvas takes touches** (a `GraphicRaycaster`) now that it has a button; its only raycast target is the pause button, so it still never takes a touch meant for the board (rule 2). A guard checks every button in every scene sits on a canvas that can hear it.
- **The pause modal uses the kit's modal and pill shapes** in the colours it had, with the result card left for slice 7.

### Refinements (2026-10-25, Milestone 4 slice 5)

Decided with the settings modal (design §8.3; the brief: sound toggles and accessibility modes, opened from the menus and from the level):

- **One `SettingsModal` prefab on every screen**, on its own canvas above everything but the pause modal (it never shows with it): switches for sound effects, music and haptics (the kit's track and knob), a close button, and a dimmed backdrop that closes it on a tap. The switches save now and take effect when haptics (slice 9) and sound (slice 10) arrive. *Why (Soni):* the settings themselves work and persist, and they get one place from the start.
- **Accessibility is a note, not a switch:** "Colour-blind friendly: every spore has its own outline." Colour-blind support is always on (design §6), so there is nothing to switch, and nothing new to save. *Why (Soni):* nothing to switch, and nothing new in the save file.
- **In a level, the settings button sits beside pause below the board** (pause left, settings right). The top-right corner by the title tab was the other candidate. *Why (Soni):* both buttons sit within thumb reach. On the menus it sits bottom-right until slice 6 lays them out anew.
- **A modal's panel takes touches** (refining rule 2): otherwise a tap on the panel between its switches would fall through to the backdrop and close the modal. The scene guard allows touch targets inside the settings modal; everywhere else only buttons take touches.
- Text: Bold only, so the open modal adds one text draw. Measured on the phone over level 11: SetPass 10 with settings open, 8 playing.

### Refinements (2026-10-25, Milestone 4 slice 6)

Decided with the main menu and level select (design §8.3; the brief's screens A and C and their mockups):

- **The slowly breathing mushroom is Play**: the title at the top, Soni's mushroom (the cap tinted #9CE8F8 over the cream stem, 360 units across) in the middle, and nothing else; the Play button and the subtitle are gone. *Why (Soni):* the mockup as drawn: the breathing mushroom is the only thing in the middle, so it reads as the way in.
- **The menus' buttons sit in a footer at the bottom centre**: settings alone on the main menu; help (?) on the left and settings on the right on level select. *Why (Soni):* as in the mockups, and within thumb reach.
- **Help (?) shows now but does nothing until onboarding exists** (slice 8). This differs from slice 4's "no button that does nothing". *Why (Soni):* nobody outside sees the game before slice 8, so a button that does nothing for a few slices costs nothing.
- **Each level is one of Soni's chunky nodes**: mint (#C3EFD8, sampled from the mockup) with the number in dark ink and three soft stars under it, earned ones gold and the rest light, all with a dark outline so they read on the light node; a locked level is the same node at 35 % opacity with the white lock instead of its number, and doesn't take taps. The number and stars, or the lock, sit centred on the node (Soni, after seeing them nudged toward the shape's top face). The mockup showed no stars and dimmed locked nodes that kept their numbers. *Why (Soni):* stars show what's done and locks show what's left, so the grid reads as progress in one look. `LevelButton` colours the node itself; the button's disabled tint is white, so the two never stack.
- **The lock is redrawn** with the shackle ending inside the body: the old one ran through and showed two bumps under it (Soni). A guard checks the lock's lowest rows are one solid edge.
- **Breathing is a small `Breathing` component, not a PrimeTween loop**: a sine ease, 3 s per breath, 5 % larger at its top, from the foot of the stem as if growing. *Why:* the scale is a pure function the tests check, and nothing is allocated or started per screen; it stops when the screen goes. The mushroom changes every frame, so it has its own nested canvas (rule 1), with its own `GraphicRaycaster`: a raycaster only reads the graphics of its own canvas, not of canvases nested in it. **Guard widened:** every button must sit on a canvas that hears taps, now its own canvas rather than its root canvas.
- Smaller choices: the grid moved 60 units up so the footer fits on a 16:9 screen; the title is 160 pt, 240 units from the top of the safe area; the mushroom darkens slightly while pressed.
- Measured on the phone: main menu SetPass 6 (the mushroom's canvas added none), level select 5, settings open 7; 0 B GC on every menu ([performance budget](../performance-budget.md)).

### Refinements (2026-10-25, Milestone 4 slice 7)

Decided with the level complete screen (design §8.3; the brief's screen E and its mockup):

- **A win fills the screen**: "Level Completed" (the brief's spelling), the stars, "Interesting Fact", the fact justified, its photo and its source, and Continue, straight on the backdrop. The board and the HUD are hidden while it shows, rather than covered, so nothing of the level shows through and nothing draws behind it. *Why (Soni):* the mockup as drawn: the fact gets the whole screen, with nothing of the level competing for attention.
- **A win still offers Continue only** (to level select), not the brief's Retry: the 2026-10-24 refinement stands. *Why (Soni):* going back to the grid after every win shows the new unlock and the stars; a replay is one tap away there.
- **The source is the short "Photo: <site>" credit**, small and left-aligned under the photo's bottom-left corner; the mockup showed the full image URL. *Why (Soni):* a URL is a source, not a licence: the credit names the site, and the full URLs stay in Soni's notes and [facts.md](../facts.md).
- **A loss is a small opaque card over the dimmed board**: "Out of moves", three dimmed stars, Retry on the left and Continue on the right. The card grew to 980 × 720 with the bigger stars (Soni: it looked cluttered), leaving at least 80 units beside and under the stars; a guard checks that room. *Why (Soni):* a loss is a quick moment, not a reward screen: a small card says "try again" without ceremony, and the dimmed stars show what was at stake.
- **The fact keeps its full text; the photo gives way.** `ResultPanel` lays the fact out between the stars and Continue: "Interesting Fact", the whole text, the photo in its own shape and its source under the photo's left edge. At the least spacing (30) the photo is as wide as the text (940) or as tall as the space left allows; a photo that would be shorter than 160 units is left out with its source rather than squeezed. This replaces the Milestone 3 card that grew with its text.
- **On taller phones the fact spreads out and sits a little above centre** (Soni, after seeing the first version leave the bottom of a tall screen empty): the spaces between heading, text and photo grow up to 80; of the height still left, the title and stars come down by 20 % and Continue rises by 30 % (both Soni, after trying it on the phone), and the rest goes 40 % above the fact and 60 % below. On a 16:9 screen nothing is left over, so the parts stay close and the title, the stars and Continue keep their places, as in the first version. The longest fact (level 3) and the tallest photo (level 5) both fit on a 16:9 screen (rendered).
- **The win's stars are 230 units** (130 in the first version; 170 and 210 were also rendered). *Why (Soni):* the stars are the reward, so they lead the screen.
- **The stars' outline is softer** (Soni picked option D of six rendered): a mid grey baked into the star, so under the UI's tint it becomes a darker shade of the star's own colour (amber on gold, grey on unearned), at 11 of 192 pixels. The same star image is used on level select, so its stars changed too. *Why (Soni):* the dark ink outline was too strong.
- Smaller choices: the kit's round buttons (retry and next icons, 150 units) in place of the green text buttons; Continue sits at the bottom centre like the menus' footers; earned stars gold, the rest white at 55 %; "Interesting Fact" is the title's font at 60 pt, the fact SemiBold 42 pt; the loss's dim reaches past the safe area like the pause overlay's.
- Measured on the phone (Soni playing, before the bigger stars and the spread-out layout, which change sizes and places only): the win SetPass 6, the loss card 10 over level 4 (9 playing), 0 B GC on both once open ([performance budget](../performance-budget.md)).

### Refinements (2026-10-26, Milestone 4 slice 8)

Decided with the intro (design §8.3; the brief's screen B and Soni's three Welcome mockups):

- **Three pages under one round Next button**: "Welcome to the world of Mycelia!", "How to Grow" and "Grow Your Network / Save the Planet", one at a time, the kit's next icon (140 units) at the bottom centre like the menus' footers; "Ready to start growing?" sits over it on the last page. No settings button: the mockups show none, so the settings-modal guard now covers every screen but the intro.
- **The text is Soni's mockup text, edited to match the game**: no points (design §3.4: stars, no score), "spores of the same kind" rather than "same colour" (types differ by outline too), the real chain rules (neighbours sideways or diagonally, two or more) and stars for moves left; the SDG page says mycelium *can help with* SDGs 9, 12 and 13 rather than that the player helps build them. *Why (Soni, drafted):* the tutorial must teach the rules the game actually plays by, and the UN's terms forbid implying that the game advances or is endorsed by its goals.
- **"How to Grow" shows the drag, animated**: a finger glides through two example chains of the game's own spores, link by link, each joining the chain as it arrives (drawn larger, as on the board), with the level's chain line behind them; on release the chain shrinks away and grows back, then the next example plays, in a loop. One example is straight, the other bends with a diagonal step, as the text says. *Why (Soni):* a chain drawing itself shows the drag gesture, which a still picture can't; new players learn it before their first level. The demo is a `ChainDemo` in the world under the camera, like the board, so it uses the board's sprites and line as they are (no copies in the UI atlas): it fits a space its page leaves for it, between the text and Next. Its timeline (`ChainDemoTimeline`) is a pure function of time, like `Breathing`, so tests check each phase and nothing is allocated or started per frame. A guard checks the example chains follow the rules (one kind, neighbours, no spore twice, one diagonal) and the line matches the level's. The mockup's left example was all red; its other column is now two other kinds, so "the same kind" shows.
- **The three UN SDG icons are committed**, in `Art/UI/SDG/` and packed in the UI atlas, the one exception to keeping third-party content out of the repository. *Why (Soni):* unlike the photos, the icons' terms are published and we follow them (unmodified, disclaimer, link), so there is no unknown licence to keep out of git. The UN allows illustrative use without asking, with its disclaimer ("The content of this publication has not been approved by the United Nations and does not reflect the views of the United Nations or its officials or Member States.") and a link to its SDG site; it forbids changing the icons or implying endorsement, and commercial use needs written permission, so the use must be checked again before any store release. The disclaimer and `un.org/sustainabledevelopment` sit under the icons on the same page, in the light text colour (the muted grey was hard to read on the turquoise). A guard checks the icons are whole, untinted, uncropped and square, with the disclaimer and the link on their page. Imported at 512 from the UN's 3000-pixel files (two are CMYK JPEGs; their colours were checked after import); the UI atlas stays one 2048 × 1024 page.
- **Laid out for a 16:9 screen first**: on taller phones the demo and the icons grow into the extra room (the icons keep their square through an `AspectRatioFitter`, two side by side over one, as in the mockup, at most 760 units wide). Titles 76–88 pt; the welcome text 60 pt and "How to Grow" 50 pt (Soni, after the first renders: too small for the room on tall phones), the SDG text 44 pt, all justified like the fact text (the welcome text 960 wide, where no line stretches); the disclaimer 26 pt. On taller phones the texts of "How to Grow" and the SDG page come down with the extra room (Soni), 120 and 80 units lower at 2400 than at 16:9, hung from a point part-way down the safe area, and what's under them follows.
- Smaller choices: pages change at once (the demo starts over each time its page shows); the finger is the smooth spore in white at 45 % over the chained spore.
- Measured on the phone (scripted taps): page 1 SetPass 6, page 2 with the demo running 8, page 3 6; 0 B GC on every page; turning a page allocates 220–320 KB in 2 frames, not a move ([performance budget](../performance-budget.md)).

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
