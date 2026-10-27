# ADR-0002: Use URP with the 2D Renderer

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-21 |
| **Deciders** | Soni Seli |

## Context

Mycelia is a 2D, sprite-based puzzle game. Its main target is a **low-end Android phone** (see the [performance budget](../performance-budget.md): stable 60 fps, ≤ 30 batches, ≤ 60 MB texture memory). *Amended 2026-10-24: the budget replaced "≤ 30 batches" with render-thread time and SetPass calls, since URP's SRP Batcher makes the batch count misleading.* It's a new project that should keep working on supported Unity versions for its whole lifetime, and its rendering needs are modest: sprites, UI, a few particle effects, and possibly a soft glow to suit the mycelium theme.

The prototype used the Built-in Render Pipeline with default sprite materials, one camera, two particle systems and no custom shaders. Nothing needs porting, so the choice can be made on its merits.

Unity has announced that the Built-in Render Pipeline is **deprecated starting in Unity 6.5**, remains supported through the Unity 6.7 LTS cycle, and is not recommended for new projects. URP is the recommended pipeline going forward.

## Options considered

### Option A: URP with the 2D Renderer
- **Pros:** Unity's recommended setup for new 2D projects. It includes the SRP Batcher, Shader Graph for sprite effects (e.g. a pulse on chain selection), and optional 2D lights that could give the network a glowing look. It's also the pipeline used by current Unity 2D samples and templates.
- **Cons:** More rendering overhead than the leanest setups, especially if lit sprites and 2D lights are used everywhere. Its settings need to be tuned for mobile.

### Option B: URP with the Universal Renderer, unlit sprites only
- **Pros:** The leanest URP setup for a game with no lighting needs.
- **Cons:** No 2D lights or 2D shadows. This is an unusual setup for a 2D game, and it closes off the glow look without measuring whether it's affordable.

### Option C: Built-in Render Pipeline
- **Pros:** Least setup, and the same as the prototype.
- **Cons:** Deprecated from Unity 6.5 and not recommended for new titles. A future migration would be guaranteed, and to a reviewer it signals a project that isn't following the platform's direction.

## Decision

We will use **URP with the 2D Renderer**. Sprites use the **unlit** sprite material by default. 2D lights and lit sprites are added only for a specific visual effect, and only after measuring that the effect fits the performance budget on the low-end reference device.

The deciding factor is **lifetime and direction**: Built-in is deprecated, and between the two URP options, the 2D Renderer keeps the glow look possible. The "unlit by default, light only after measuring" rule removes most of its cost risk.

### Refinements (2026-10-27, Milestone 5 slice 1)

- **The 2D renderer carries no post-process data.** No camera ever used post-processing, but the renderer still referenced URP's `PostProcessData`, so every build shipped its film grain and SMAA textures and post-processing shaders (−2.0 MiB of APK without them), and the trims it came with took about 19 MB off memory ([performance budget](../performance-budget.md), case study "Seven megabytes the game never used"). `ProjectBuildSizeTests` keeps every renderer without it; a post-processing effect would bring it back, measured. *Why (Soni):* it costs size and memory, and nothing uses it.

## Consequences

- **Positive:**
  - The project follows Unity's supported direction, so no pipeline migration will be needed later.
  - Visual polish (glow, pulses) can be done in Shader Graph and 2D lights without changing pipelines.
  - The SRP Batcher and sprite atlases help keep batches within budget.
- **Negative:**
  - The URP and 2D Renderer assets must be tuned for mobile. The defaults are not tuned for a low-end phone.
  - Any lit-sprite effect has a measurable cost that must be justified in the performance log.
- **Follow-up:**
  - Create the Unity project from the **Universal 2D** template in Milestone 0.
  - Set up a mobile URP asset: HDR off, MSAA off (sprites don't need it), post-processing off unless a measured effect needs it, render scale 1.0, and a minimal set of 2D light blend styles.
  - Use `Sprite-Unlit-Default` as the project's default sprite material.
  - Record batch count and GPU frame time for the gameplay scene in the [results log](../performance-budget.md#results-log) once the board view exists (Milestone 2).
  - **Revisit if:** the 2D Renderer alone, with no lights, cannot meet the frame-time budget on the reference device. Switching to Option B is cheap because unlit sprites work in both renderers.

## Evidence

- Prototype audit (2026-10-21): Built-in pipeline, 2 custom materials (both using a default built-in shader), 1 camera in the Game scene, 2 particle systems, no custom shaders outside TextMesh Pro.
- Unity, [Render Pipelines strategy for 2026](https://unity.com/topics/render-pipelines-strategy-for-2026): Built-in deprecated in Unity 6.5; URP recommended for new projects.
- Unity Manual (6.6), [Set up a render pipeline](https://docs.unity3d.com/6000.6/Documentation/Manual/render-pipelines-set-up.html): Built-in remains supported through the Unity 6.7 LTS cycle.
