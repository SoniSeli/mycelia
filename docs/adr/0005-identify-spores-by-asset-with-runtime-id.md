# ADR-0005: Identify spore types by ScriptableObject asset, with a compact runtime id and a stable key

| | |
|---|---|
| **Status** | Accepted (resume-related parts amended by [ADR-0006](0006-save-progress-as-versioned-json.md)) |
| **Date** | 2026-10-21 |
| **Deciders** | Soni Seli |
| **Related** | [ADR-0003](0003-separate-pure-domain-from-views.md) (engine-free domain), [ADR-0004](0004-resolve-moves-through-a-phase-pipeline.md) (typed steps) |

> **Amended by [ADR-0006](0006-save-progress-as-versioned-json.md): no mid-level resume in v1.** The core decision stands: ScriptableObject assets, a 1-byte runtime `SporeTypeId` in the domain, a stable `Key`, and always-on silhouettes. What changes:
> - Spore keys are **no longer persisted**. Saves contain only progress and settings. Keys are still used for analytics and debugging, and keep the door open for resume later.
> - The resume board format (key palette + cell indices) and unknown-key handling below are **not built in v1**. They are kept here as the design to start from if resume is revisited.
> - The `shipped-spore-keys.txt` guard test is **deferred** until something persists spore keys. Unique-key and unique-silhouette validation still apply.
>
> The original text is unchanged below, for the record.

## Context

Every rule in [design §3](../design.md#3-game-rules) compares spore types: chains must be the same type, refill picks from the level's types, objectives count a type. The prototype identified types **by colour**, comparing floats with a tolerance. That was fragile, mixed identity with presentation, and made colour-blind support impossible without changing the gameplay code.

Four parts of the game need to refer to a spore type, each with different requirements:

| Who | Needs |
|---|---|
| **Domain** (engine-free, hot path) | Cheap equality, compact storage, no Unity types, no GC |
| **Level authoring** (Editor) | Drag-and-drop references that survive renames; adding a type without code |
| **Persistence** (mid-level resume) and analytics | An id that stays the same **across app versions** |
| **View** | Colour, sprite, accessible shape, label |

Two related product decisions were made at the same time:
- **Colour-blind support is always on.** Every spore type is a distinct fungus species with its own silhouette, so colour is never the only cue.
- **A level in progress survives the app being killed.** The board must be saved and restored, so spore identity has to be persisted.

## Options considered

### Option A: ScriptableObject assets, mapped to a compact runtime id; a stable key for persistence
- **Pros:** Designers work with assets (GUID references survive renames). The domain sees only a 1-byte id with trivial equality. Saves and analytics use a human-readable key that never changes. Visuals live with the type.
- **Cons:** A mapping layer (catalog → ids) and three representations of one concept, each with a clear owner.

### Option B: C# enum
- **Pros:** Simplest, compile-time safety, no mapping.
- **Cons:** Adding a type is a code change. Unity serializes enums as integers, so reordering or removing a member silently corrupts level data and saves. Visuals still need a separate lookup.

### Option C: String keys throughout, including the domain
- **Pros:** Readable and data-driven.
- **Cons:** String comparisons and typo risk in the gameplay hot path, and harder to keep within the 0 B GC budget.

## Decision

We will use **Option A**:

1. **`SporeType` (ScriptableObject) is the source of truth** for a type. It holds:
   - `Key`: a stable, lowercase string such as `"amber"`. Unique, and **never changed once shipped**.
   - Display name (the species name).
   - Colour, species sprite (distinct silhouette), and an accessible label.
2. **`SporeCatalog` (ScriptableObject)** lists every type. At load, it assigns each one a runtime **`SporeTypeId`**: a `byte` wrapper, where `0` is reserved for *none*. The id is **runtime-only**: never saved, sent or shown.
3. **The domain uses only `SporeTypeId`.** The board stores one byte per cell, and steps carry ids. `LevelDefinition` assets reference `SporeType` assets, and the App-layer mapper converts them into `LevelConfig` ids.
4. **Persistence and analytics use `Key`.** A resumed board is saved as a palette of keys plus per-cell indices into that palette, so the save is compact and doesn't depend on catalog order.
5. **Colour-blind support is built into the art, not a setting.** Each `SporeType` must have a unique silhouette. A validation test enforces it.

The deciding factor is that **each part of the game gets the representation it needs**: the domain a byte, designers an asset, saves a stable key. Each conversion happens at a single, tested boundary.

### Shape (illustrative)

```csharp
// Mycelia.Domain
public readonly struct SporeTypeId : IEquatable<SporeTypeId>
{
    public static readonly SporeTypeId None = default;
    public readonly byte Value;
    // Equals / GetHashCode / ==, no boxing
}

// Mycelia.App (Unity)
[CreateAssetMenu(menuName = "Mycelia/Spore Type")]
public sealed class SporeType : ScriptableObject
{
    [SerializeField] string key;            // "amber": stable forever once shipped
    [SerializeField] string displayName;    // species name
    [SerializeField] Color color;
    [SerializeField] Sprite sprite;         // unique silhouette per type
    [SerializeField] string accessibleLabel;
}

public sealed class SporeCatalog : ScriptableObject
{
    [SerializeField] List<SporeType> types;
    public SporeTypeId IdOf(SporeType type);    // built once at load
    public SporeType TypeOf(SporeTypeId id);    // for views
    public bool TryGetByKey(string key, out SporeTypeId id);   // for save restore
}

// Resume save (part of the save format, decided in a later ADR)
// { "palette": ["amber", "coral", "violet", "mint"], "cells": [0,1,1,3, ...], ... }
```

### Validation (an Editor check and an EditMode test)

- Every `SporeType` has a non-empty, lowercase, **unique** `Key`, and the catalog has at most 255 types.
- Every `SporeType` has a sprite, and **no two types share a sprite**. This is the colour-blind guarantee.
- Every type a level references is in the catalog, and every objective's type is one of the level's types (from [design §3.5](../design.md#35-objectives-winning-and-losing)).
- A list of shipped keys (`shipped-spore-keys.txt`) is checked into the repo. A test fails if any shipped key disappears from the catalog. Removing a type is a deliberate act, not an accident.

### Refinements (2026-10-25, Milestone 4 slice 3)

Decided with the new spores (design §8.3, Soni's UI/UX brief: "circles, differentiated by their outlines, not just colour"):

- **The silhouettes are four circles with different outlines:** smooth, scalloped (eight round bumps), spiked (twelve short points) and **notched** (three round bites out of the edge). A ring (a circle with a hole) was the other candidate for the fourth and stood out most at a glance. *Why (Soni):* a notched spore stays a full circle, as the brief asks; a ring adds a hole, which is a different shape rather than a different outline. Drawn for the project as anti-aliased distance shapes, white with a darker lower rim and a soft glow (the mockup's look), so the type's colour tints all of it.
- **The colours are the mockup's**, sampled from it: red `#F57270` (smooth), purple `#CB89FF` (scalloped; the brief's "blue"), green `#6DE58A` (spiked), orange `#F1B54C` (notched). Compared before deciding, in CIELAB distance under colour-blindness simulations (Machado et al. 2009) and against the backdrop where the board sits: the Okabe–Ito set in place since Milestone 2 kept its colours further apart for colour-blind players (closest pair 16–21 against the mockup's 13 for deuteranopia), but two of its colours nearly vanished on the new blue backdrop (contrast 1.1–1.2:1 at the board's bottom); a tuned version of the mockup's colours spread their brightness more (closest pair 20–21). *Why (Soni):* the mockup's colours are brighter on the blue backdrop. No colour carries identity on its own: red and green come close for deuteranopia and red and purple in grayscale, and each of those pairs has different outlines.
- **The keys follow the outlines:** `spore-smooth`, `spore-scalloped`, `spore-spiked`, `spore-notched` (were `spore-circle`, `-diamond`, `-square`, `-triangle`). The assets were renamed in place, so levels keep their references. *Why (Soni):* nothing shipped stores a spore key yet (saves and analytics name levels only), so this is the last cheap moment for the keys to say what players see.
- **The silhouette guard now reads the pixels**, since four circles could pass "no two types share a sprite" while looking alike. `ProjectSporeArtTests` reads each shipped sprite's opaque body (the glow is fainter than the threshold) and checks that every two spores differ in more than 4 % of their combined area (the closest pair, smooth and notched, differs in 7 %), that each one is a circle centred on its cell that its outline only changes, and that all four are medium-sized and cover about the same area ([ADR-0011](0011-fit-camera-to-board-and-pool-views-up-front.md) Refinements).
- The renders of the game screen in the three simulations and grayscale showed the four types apart in each (design §6).

### Refinements (2026-10-27, Milestone 5 planning)

- **The spore types are not named fungus species.** Outline and colour are their identity, and the key names the outline. The `displayName` field stays, but no screen shows it. *Why (Soni):* the four types have no species.

## Consequences

- **Positive:**
  - Fixes the prototype's colour-matching bug at the root: identity and presentation are fully separate.
  - The hot path stays cheap: 36 bytes for a 6 × 6 board, integer equality, and steps that remain small structs.
  - Adding a spore type means creating an asset and its art. No code changes.
  - Saves and analytics stay readable (`"amber"`) and stable across releases, even if the catalog is reordered.
  - Accessibility is the default experience. Colour-blind players don't have to find a setting, and the species silhouettes fit the educational theme (each species can have its own fact).
- **Negative:**
  - One concept, three representations (asset, runtime id, key). Mitigated by keeping each conversion in exactly one place (`SporeCatalog` and the level mapper) and testing both.
  - **Art cost:** at least 4 visually distinct species silhouettes, readable at phone size. The prototype's single bubble shape can't be reused for identity.
  - A shipped key can never be renamed. If a type must go, its key stays reserved, and a save containing an unknown key is handled as below.
- **Follow-up:**
  - **Save format ADR:** must store the resumed board using keys and handle an **unknown key** (for example, a type removed in an update). Proposed behaviour: drop the resumed level, restart it cleanly, and log an analytics event. Never crash, and never substitute a random type.
  - **Art:** design 4 species silhouettes and test them at actual phone size, and in grayscale and colour-blindness simulations (protanopia, deuteranopia, tritanopia), before final art.
  - Milestone 1: `SporeTypeId` and text-grid fixtures (letters map to ids in tests). Milestone 3: `SporeCatalog`, the level mapper and the validation tests.
