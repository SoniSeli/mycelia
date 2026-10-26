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
- **`SceneLoader` applies the frame rate**, from a per-`SceneId` table (`FrameRatePolicy`: Game 60 fps, everything else 30 fps), before it calls `Construct`. Every scene is covered without each entry remembering to do it. An entry may still lower it while idle, e.g. under the pause overlay (slice 7). *Done in slice 7: `GameSceneEntry` gives `LevelPause` a `PausedFrameRate`, so a paused game idles at 30 fps ([ADR-0013](0013-pause-the-level-and-handle-back.md)).* *Replaced 2026-10-26 (Milestone 4 slice 11): `GameFrameRate`, which also idles the result screens (Refinements below).*
- **Until Level Select exists, a build opens the Game scene** with the level set in AppRoot's *Start Level* field. There is no `LevelCatalog` yet. *Replaced 2026-10-24 (Milestone 3 slice 1): AppRoot holds the `LevelCatalog`, passes it on in `AppServices`, and opens its first level ([ADR-0006](0006-save-progress-as-versioned-json.md) Refinements).* *Replaced again 2026-10-24 (slice 4): a build opens the `MainMenu` scene; levels are chosen on `LevelSelect` ([ADR-0012](0012-use-ugui-with-textmeshpro.md) Refinements).*
- **Entry timing:** the loader calls `Construct` from `SceneManager.sceneLoaded`, so after every Awake and OnEnable in the new scene and before any Start. Bootstrap is never loaded again, and a second load while one is in progress is rejected.
- **Editor Play:** `playModeStartScene` is set to Bootstrap only while entering Play from a saved scene, and cleared afterwards. The scene's path is handed to AppRoot through `SessionState`, and AppRoot reopens it with no `SceneArgs`, so the scene uses its debug args (`GameSceneEntry.debugLevel`). Unsaved edits are offered for saving first, since Bootstrap loads scenes from disk. An untitled scratch scene plays as it is.
- **Guards:** EditMode tests check that Bootstrap is build index 0 with one `AppRoot` whose assets are set, that every `SceneId` is enabled in the build, and that every other scene has exactly one entry.

### Refinements (2026-10-25, Milestone 4 slice 2)

Decided with the scene fade (design §8.3):

- **Every scene change fades through the backdrop's gradient**, 0.25 s out and 0.25 s in, and the first screen fades in too: the fade starts covered when the app launches. *Why (Soni):* half a second per screen change is noticeable but quick, and the first screen arrives the same way as every other. The fade first went through the gradient's deep blue (*why:* moving between two blue screens never flashes black); after seeing it, Soni chose the whole gradient. *Why (Soni):* a fade then looks like the background with the screen's content gone, so changing screens feels like moving within one space. `GradientGraphic` draws it in the backdrop's rows, blended the same way; headless renders of the two match within 2 levels of 255 (5 in the top row, which the backdrop overshoots on purpose).
- **The fade belongs to the composition root.** A `ScreenFade` overlay sits under the Bootstrap's `AppRoot`, so it outlives every scene, and draws above every canvas. `AppRoot` hands it to the `SceneLoader`; scene entries don't know about it. Once the screen is clear, its canvas is switched off, so it costs nothing during play.
- **`SceneTransition` holds the order of a change**, engine-free and tested: fade out, load once the screen is covered, build the scene through its entry, fade in. `SceneLoader` is only the glue to Unity's scene loading.
- **Nothing can be pressed while the screen changes.** The fade catches UI taps until it is clear again. `ISceneLoader` reports the change as an input block, so the board ignores a touch that starts during it until the finger lifts. A second request during a change (a second tap, or back) is ignored: the first wins. Only the new scene's own `Construct` may open another scene, and the screen stays covered for it. This replaces the old exception for a load requested while another was loading. It also fixed a quick tap on Play starting the level whose button sat under the same spot on level select, a tap that carried across the scene change (found on the phone in slice 1, checked gone there).

### Refinements (2026-10-26, Milestone 4 slice 8)

Decided with the intro (design §8.3):

- **The intro is a scene of its own, `Intro`, opened after the first tap on the mushroom**: the main menu stays the first screen; while the save says the intro was never finished, Play opens the intro instead of level select, and Next on its last page leads into level select. Level select's help (?) replays it, and the replay ends in level select too. *Why (Soni, drafted):* the title screen stays the game's front door, and the last page's "Ready to start growing?" leads straight into the levels; both ways in end in the same place. A scene of its own (with the usual fade) keeps level select's canvas from drawing under it.
- **`IntroSceneArgs` says whether it is the first run**; the Editor's Play in the Intro scene opens it as a replay. Back turns back a page; on the first page it returns where the intro came from (the main menu on a first run, level select on a replay). There is no skip button. *Why (Soni, drafted):* it's only three pages, and back already leaves.
- **Only finishing counts as seen**: `PlayerProgress.MarkIntroSeen` and a save when Next is pressed on the last page (ADR-0006 Refinements). Leaving early shows it again on the next Play.
- **No analytics event for the intro** (ADR-0014). *Why (Soni):* not needed now; add it in Milestone 5 if playtests show people skip the intro.

### Refinements (2026-10-26, Milestone 4 slice 9)

- **`AppServices.Haptics`** is the device's haptics behind the saved switch (`SwitchedHaptics` over `AndroidHaptics.Create()`), created once in `AppRoot`; the Game scene's entry wraps the chain view, the move player and the win wave with it, and every settings modal gets it for its test tick ([ADR-0016](0016-drive-android-haptics-through-the-vibrator.md)). The sketch's `AndroidHaptics` service is now real; audio is still to come (slice 10).

### Refinements (2026-10-26, Milestone 4 slice 10)

- **`AppServices.Sounds`** is a `SwitchedSoundEffects` over the `AppRoot`'s `AudioPlayer`, created once in `AppRoot`; the Game scene's entry wraps the chain and the outcome with it and registers it with `LevelPause`, every scene entry attaches a tap to every button in its scene, and every settings modal gets it for the switch tap ([ADR-0017](0017-play-sound-with-playoneshot-behind-a-sound-interface.md)). The music isn't a service: `AppRoot` owns a `Music` and calls `Follow()` every frame. The sketch's `AudioService` is now real.
- **The app's one `AudioListener` lives on the `AppRoot`**, next to the `AudioPlayer`, so it outlives scene loads; the Bootstrap and Game cameras no longer carry one (the menus had none before). Guarded by `ProjectScenesTests`.

### Refinements (2026-10-26, Milestone 4 slice 11)

Decided in the device pass, from measurements on the Redmi ([performance budget](../performance-budget.md)):

- **The result screens idle at 30 fps**, like the pause overlay. `GameFrameRate` replaces `PausedFrameRate`: it is the `IPausable` that `LevelPause` tells, and it also sits between the win's wave and the HUD as the playback listener, so the game drops to the menu rate once the result shows (after the wave, which still plays at 60 fps) and stays there through a pause and resume. `FrameRatePolicy.TargetFor(scene, idle)` covers both. On the phone each result frame cost the same at 30 and 60 fps (win SetPass 6, GPU about 5 ms; loss card SetPass 10, GPU 5.9 ms, main 4.2 ms). *Why (Soni):* the result screens are still, like the pause overlay; at 30 fps the phone does half the work for the same picture, and the budget already runs idle screens at 30 fps.
- **The Unity splash screen is off.** It added about 2 s before the first scene on every launch: cold start to level select took about 3.8 s with it and about 1.85 s without (budget 3 s). Turning it off costs about 11 MB more GPU memory (GL mtrack), which puts level 11 at about 305 MB against the 300 MB budget, to be won back in Milestone 5's memory pass. Guarded by `ProjectPlayerSettingsTests`. *Why (Soni):* every player waits for a slow start on every launch, while the extra memory is small and the Milestone 5 memory pass can win it back (High stripping alone measured −19 MB).

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
