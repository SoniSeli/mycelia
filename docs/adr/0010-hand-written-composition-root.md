# ADR-0010: Wire services with a hand-written composition root in the Bootstrap scene

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-22 |
| **Deciders** | Soni Seli |
| **Related** | [design §5](../design.md#5-architecture-proposal) (one bootstrap scene creates the services), [ADR-0006](0006-save-progress-as-versioned-json.md) (`IProgressStore`) |

## Context

The App layer needs a handful of long-lived services: progress saving (ADR-0006), audio, haptics, scene loading, analytics, a seed source (ADR-0007) and the level and spore catalogs (ADR-0005). Scenes need them: the Game scene needs a level, a seed, playback settings, haptics and audio; Level Select needs progress.

The prototype used singletons loaded from `Resources` and passed the chosen level through a static field. That made dependencies invisible and tests hard, and it's the pattern the rewrite set out to replace.

## Options considered

### Option A: Hand-written composition root
- **Pros:** Every dependency is created in one file and passed explicitly; no framework or reflection; easy to read and explain; fakes are trivial in tests.
- **Cons:** Wiring is manual: adding a service means editing the root and the scenes that need it.

### Option B: VContainer
- **Pros:** Fast, Unity-specific DI with lifetime scopes; used in industry; recognisable on a CV.
- **Cons:** A dependency and a learning curve for about six services. Scopes and registration indirection make the flow harder to follow for a small project.

### Option C: Singletons or a service locator
- **Pros:** Least code.
- **Cons:** Hidden dependencies, global state, hard to fake in tests: exactly what the prototype did.

## Decision

We will use a **hand-written composition root (Option A)**.

The deciding factor is **proportion**: with about six services and three scenes, explicit wiring is shorter and clearer than any framework, and every dependency stays visible in one place.

### Shape

```csharp
// Bootstrap scene (build index 0). Created once, survives scene loads.
public sealed class AppRoot : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        var services = new AppServices(
            progress:  new JsonProgressStore(Application.persistentDataPath),
            audio:     new AudioService(...),
            haptics:   new AndroidHaptics(),
            analytics: new NullAnalytics(),          // real backend later
            seeds:     new CryptoSeedSource(),        // ADR-0007
            levels:    levelCatalog,                  // ScriptableObjects (ADR-0005)
            scenes:    new SceneLoader(...));
        services.Scenes.Load(SceneId.LevelSelect, services);
    }
}

// Each scene has exactly one entry point; the loader calls Construct after the scene loads.
public interface ISceneEntry
{
    void Construct(AppServices services, SceneArgs args);
}
```

- **`AppServices`** is a plain class holding the service *interfaces*. It's passed explicitly; there is no static `Instance`.
- **Scene arguments** (for example, which level to play) are passed in `SceneArgs` through the loader. No static fields carry state between scenes.
- **Inside a scene**, the entry point creates or initialises its objects and hands each one what it needs through `Init(...)` or `Construct(...)` methods. There are no `FindObjectOfType` calls and no singletons.
- **Playing from any scene in the Editor:** an Editor-only script sets `EditorSceneManager.playModeStartScene` to Bootstrap, so pressing Play always goes through `AppRoot`. A per-scene "debug args" field picks the level to open.
- **Frame rate policy** lives in the root and scene entries: 60 fps in gameplay, 30 fps in menus (performance budget).

### Refinements (2026-10-23, Milestone 2 slice 2)

Decided while building the bootstrap:

- **Scene entries live in `Mycelia.App`**, next to `AppServices`. They are composition code: Game-layer views never see `AppServices` and get only what they need through `Init(...)`, so `Mycelia.Game` doesn't depend on the service interfaces.
- **`SceneLoader` applies the frame rate**, from a per-`SceneId` table (`FrameRatePolicy`: Game 60 fps, everything else 30 fps), before it calls `Construct`. Every scene is covered without each entry remembering to do it. An entry may still lower it while idle, e.g. under the pause overlay (slice 7). *Done in slice 7: `GameSceneEntry` gives `LevelPause` a `PausedFrameRate`, so a paused game idles at 30 fps ([ADR-0013](0013-pause-the-level-and-handle-back.md)).*
- **Until Level Select exists, a build opens the Game scene** with the level set in AppRoot's *Start Level* field. There is no `LevelCatalog` yet. *Replaced 2026-10-24 (Milestone 3 slice 1): AppRoot holds the `LevelCatalog`, passes it on in `AppServices`, and opens its first level ([ADR-0006](0006-save-progress-as-versioned-json.md) Refinements).* *Replaced again 2026-10-24 (slice 4): a build opens the `MainMenu` scene; levels are chosen on `LevelSelect` ([ADR-0012](0012-use-ugui-with-textmeshpro.md) Refinements).*
- **Entry timing:** the loader calls `Construct` from `SceneManager.sceneLoaded`, so after every Awake and OnEnable in the new scene and before any Start. Bootstrap is never loaded again, and a second load while one is in progress is rejected.
- **Editor Play:** `playModeStartScene` is set to Bootstrap only while entering Play from a saved scene, and cleared afterwards. The scene's path is handed to AppRoot through `SessionState`, and AppRoot reopens it with no `SceneArgs`, so the scene uses its debug args (`GameSceneEntry.debugLevel`). Unsaved edits are offered for saving first, since Bootstrap loads scenes from disk. An untitled scratch scene plays as it is.
- **Guards:** EditMode tests check that Bootstrap is build index 0 with one `AppRoot` whose assets are set, that every `SceneId` is enabled in the build, and that every other scene has exactly one entry.

## Consequences

- **Positive:**
  - All dependencies are visible in `AppRoot` and the scene entries; nothing is global.
  - Services are interfaces, so PlayMode tests can construct a scene entry with fakes.
  - No framework or reflection cost at startup.
- **Negative:**
  - Manual wiring: a new service touches the root and the entries that use it.
  - Discipline is needed to avoid shortcuts such as a static `Instance` creeping back in. Code review and the design doc are the guard.
- **Follow-up:**
  - Milestone 2: `AppRoot`, `AppServices`, `SceneLoader`, `ISceneEntry`, the Editor play-mode start scene, and the Game scene entry that builds `LevelSession` and the views. *Done in slice 2 except the views, which are handed the session as their slices land.*
  - Slice 5 (first PlayMode tests): check that the Play redirect doesn't take over the Test Runner's own play-mode scene.
  - **Revisit if:** the service count grows beyond about 15, or scoping (per-level or per-session services) becomes complicated. VContainer (Option B) would then be worth its cost.
