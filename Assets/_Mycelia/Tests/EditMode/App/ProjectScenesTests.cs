using System;
using System.IO;
using System.Linq;
using Mycelia.App.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mycelia.App.Tests
{
    /// <summary>Guards the real scenes and build settings, so a broken boot fails CI instead of the app (ADR-0010).</summary>
    public class ProjectScenesTests
    {
        static SceneId[] AllScenes => (SceneId[])Enum.GetValues(typeof(SceneId));

        // Every scene the player sees; Bootstrap has no UI.
        static SceneId[] UiScenes => AllScenes.Where(scene => scene != SceneId.Bootstrap).ToArray();

        static string PathOf(SceneId scene) =>
            EditorBuildSettings.scenes.Single(s => Path.GetFileNameWithoutExtension(s.path) == scene.ToString()).path;

        // Opens a scene alongside whatever is open, and closes it afterwards unless it was already open.
        static void WithScene(SceneId id, Action<GameObject[]> check)
        {
            string path = PathOf(id);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasOpen = scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                check(scene.GetRootGameObjects());
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        [Test]
        public void Bootstrap_IsTheFirstSceneInTheBuild()
        {
            Assert.That(Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[0].path), Is.EqualTo(nameof(SceneId.Bootstrap)));
        }

        [Test]
        public void EveryScene_IsEnabledInTheBuild()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path));

            Assert.That(AllScenes.Select(scene => scene.ToString()), Is.SubsetOf(enabled));
        }

        [Test]
        public void Bootstrap_HasOneAppRoot_WithItsAssetsSet()
        {
            WithScene(SceneId.Bootstrap, roots =>
            {
                var appRoots = roots.SelectMany(root => root.GetComponentsInChildren<AppRoot>(true)).ToArray();
                Assert.That(appRoots.Length, Is.EqualTo(1));

                var serialized = new SerializedObject(appRoots[0]);
                Assert.That(serialized.FindProperty("spores").objectReferenceValue, Is.Not.Null, "Spores");
                Assert.That(serialized.FindProperty("levels").objectReferenceValue, Is.Not.Null, "Levels");
            });
        }

        // Design §8.3 slice 2: the fade outlives every scene, and covers all of every screen, pause overlay included.
        [Test]
        public void Bootstrap_FadesBetweenScenes_OnTheAppRoot_AboveEveryCanvasOfEveryScreen()
        {
            int highest = int.MinValue;
            foreach (SceneId scene in UiScenes)
                WithScene(scene, roots => highest = Math.Max(highest,
                    roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Where(c => c.isRootCanvas).Max(c => c.sortingOrder)));

            WithScene(SceneId.Bootstrap, roots =>
            {
                AppRoot appRoot = roots.SelectMany(root => root.GetComponentsInChildren<AppRoot>(true)).Single();
                var fade = (Game.ScreenFade)new SerializedObject(appRoot).FindProperty("fade").objectReferenceValue;
                Assert.That(fade, Is.Not.Null);
                Assert.That(fade.transform.IsChildOf(appRoot.transform), Is.True, "It outlives scene loads with the AppRoot.");

                var canvas = (Canvas)new SerializedObject(fade).FindProperty("canvas").objectReferenceValue;
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.sortingOrder, Is.GreaterThan(highest));
                Assert.That(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null, "It catches taps while it shows.");
                Assert.That(canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Any(graphic => graphic.raycastTarget), Is.True);
            });
        }

        // Soni's choices: the backdrop's gradient, 0.25 s each way, and the first screen fades in from it.
        [Test]
        public void TheFade_StartsCovered_InTheBackdropsGradient_AQuarterSecondEachWay()
        {
            var backdrop = new SerializedObject(AssetDatabase.LoadAssetAtPath<Game.Backdrop>("Assets/_Mycelia/Prefabs/Backdrop.prefab"));
            WithScene(SceneId.Bootstrap, roots =>
            {
                Game.ScreenFade fade = roots.SelectMany(root => root.GetComponentsInChildren<Game.ScreenFade>(true)).Single();
                var serialized = new SerializedObject(fade);
                var group = (CanvasGroup)serialized.FindProperty("group").objectReferenceValue;
                var canvas = (Canvas)serialized.FindProperty("canvas").objectReferenceValue;

                Assert.That(group.alpha, Is.EqualTo(1f));
                Assert.That(group.blocksRaycasts, Is.True);
                Assert.That(canvas.enabled, Is.True);
                Assert.That(serialized.FindProperty("duration").floatValue, Is.EqualTo(0.25f));
                UnityEngine.UI.Graphic cover = canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Single();
                Assert.That(cover, Is.InstanceOf<Game.GradientGraphic>());
                var gradient = new SerializedObject(cover);
                Assert.That((Color32)gradient.FindProperty("top").colorValue, Is.EqualTo((Color32)backdrop.FindProperty("top").colorValue), "top");
                Assert.That((Color32)gradient.FindProperty("bottom").colorValue, Is.EqualTo((Color32)backdrop.FindProperty("bottom").colorValue), "bottom");
                Assert.That((Color32)cover.color, Is.EqualTo((Color32)Color.white), "No tint on top of the gradient.");
            });
        }

        // Performance budget: development builds log frame time, GC and batches from the first frame to the last.
        [Test]
        public void Bootstrap_ProbesPerformance_OnTheAppRoot_WhichOutlivesEveryScene()
        {
            WithScene(SceneId.Bootstrap, roots =>
            {
                AppRoot appRoot = roots.SelectMany(root => root.GetComponentsInChildren<AppRoot>(true)).Single();
                Assert.That(appRoot.GetComponent<PerfProbe>(), Is.Not.Null);
            });
        }

        [Test]
        public void Bootstrap_HasNoSceneEntry()
        {
            WithScene(SceneId.Bootstrap, roots =>
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<ISceneEntry>(true)), Is.Empty));
        }

        [Test]
        public void EveryOtherScene_HasExactlyOneEntry()
        {
            foreach (SceneId scene in AllScenes.Where(scene => scene != SceneId.Bootstrap))
            {
                WithScene(scene, roots => Assert.DoesNotThrow(() => SceneEntries.FindSingle(scene.ToString(), roots)));
            }
        }

        [Test]
        public void TheGameScene_HasADebugLevel_SoPlayWorksFromIt()
        {
            WithScene(SceneId.Game, roots =>
            {
                var entry = (GameSceneEntry)SceneEntries.FindSingle("Game", roots);
                Assert.That(new SerializedObject(entry).FindProperty("debugLevel").objectReferenceValue, Is.Not.Null);
            });
        }

        [Test]
        public void EveryMyceliaComponent_InEveryScene_HasItsReferencesSet()
        {
            foreach (SceneId scene in AllScenes)
            {
                WithScene(scene, roots =>
                {
                    var components = roots
                        .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                        .Where(component => component.GetType().Namespace?.StartsWith("Mycelia") == true);
                    foreach (MonoBehaviour component in components)
                    {
                        SerializedProperty property = new SerializedObject(component).GetIterator();
                        while (property.NextVisible(enterChildren: true))
                        {
                            // uGUI's own Graphic.m_Material: empty means the default UI material.
                            bool graphicMaterial = component is UnityEngine.UI.Graphic && property.name == "m_Material";
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script" && !graphicMaterial)
                                Assert.That(property.objectReferenceValue, Is.Not.Null, $"{scene}: {component.name}.{property.name}");
                        }
                    }
                });
            }
        }

        [Test]
        public void TheBoard_SitsAtTheWorldOrigin_WhereTheLayoutExpectsIt()
        {
            WithScene(SceneId.Game, roots =>
            {
                Game.BoardView board = roots.SelectMany(root => root.GetComponentsInChildren<Game.BoardView>(true)).Single();
                Assert.That(board.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(board.transform.lossyScale, Is.EqualTo(Vector3.one));
            });
        }

        // ADR-0012 rule 2: the HUD must never swallow a touch meant for the board, and menus take touches only on buttons.
        [TestCaseSource(nameof(UiScenes))]
        public void OnlyButtons_TakeTouches(SceneId scene)
        {
            WithScene(scene, roots =>
            {
                // A modal's panel takes touches too, so a tap on it doesn't reach the backdrop that closes the modal.
                var touchable = roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    .Where(graphic => graphic.raycastTarget && graphic.GetComponentInParent<UnityEngine.UI.Selectable>(true) == null
                        && graphic.GetComponentInParent<Game.SettingsModal>(true) == null);
                Assert.That(touchable.Select(graphic => graphic.name), Is.Empty);
            });
        }

        // A button on a canvas without a raycaster draws but never hears a tap.
        [TestCaseSource(nameof(UiScenes))]
        public void EveryButton_IsOnACanvasThatTakesTouches(SceneId scene)
        {
            WithScene(scene, roots =>
            {
                foreach (UnityEngine.UI.Button button in roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Button>(true)))
                {
                    // The button's own canvas: a raycaster reads only the graphics of its canvas, not of canvases nested in it.
                    Canvas canvas = button.GetComponentInParent<Canvas>(true);
                    bool hears = canvas.TryGetComponent(out UnityEngine.UI.GraphicRaycaster raycaster) && raycaster.enabled;
                    Assert.That(hears, Is.True, $"{button.name} on {canvas.name}");
                }
            });
        }

        // Design §8.3 (Milestone 4 slice 4): one card in the top band holding moves left and the goals together, and the pause
        // button outside it, in the safe area, so HudView can place it below the board.
        [Test]
        public void TheHud_HasOneCardForMovesAndGoals_AndThePauseButtonOutsideTheBand()
        {
            WithScene(SceneId.Game, roots =>
            {
                Game.HudView hud = roots.SelectMany(root => root.GetComponentsInChildren<Game.HudView>(true)).Single();
                var serialized = new SerializedObject(hud);
                var band = (RectTransform)serialized.FindProperty("band").objectReferenceValue;
                var pause = (RectTransform)((Component)serialized.FindProperty("pauseButton").objectReferenceValue).transform;
                var moves = (RectTransform)((Component)serialized.FindProperty("movesText").objectReferenceValue).transform;
                var goals = (RectTransform)serialized.FindProperty("slotsRoot").objectReferenceValue;

                // The card: the nearest background behind both moves and goals.
                RectTransform card = moves.GetComponentsInParent<RectTransform>(true)
                    .FirstOrDefault(parent => goals.IsChildOf(parent) && parent.TryGetComponent(out UnityEngine.UI.Image _));
                Assert.That(card, Is.Not.Null, "Moves and goals share a card with a background.");
                Assert.That(card.IsChildOf(band) && card != band, Is.True, "The card sits inside the band.");
                Assert.That(pause.IsChildOf(band), Is.False, "The pause button sits outside the top band.");
                Assert.That(pause.parent, Is.SameAs(band.parent), "It shares the band's parent, the safe area.");
                Assert.That(pause.GetComponent<UnityEngine.UI.Button>(), Is.Not.Null);

                Rect bandRect = WorldRect(band), cardRect = WorldRect(card);
                Assert.That(cardRect.yMin, Is.GreaterThanOrEqualTo(bandRect.yMin - 1f), "The card stays in the band.");
                Assert.That(cardRect.yMax, Is.LessThanOrEqualTo(bandRect.yMax + 1f), "The card stays in the band.");
            });
        }

        // Performance budget (SetPass <= 10, paused included; measured on the phone 2026-10-25): every HUD text sits on the
        // counters canvas in one font, so all of it draws as one batch instead of one per canvas.
        [Test]
        public void EveryHudText_SharesTheCountersCanvas_AndOneFont()
        {
            WithScene(SceneId.Game, roots =>
            {
                Game.HudView hud = roots.SelectMany(root => root.GetComponentsInChildren<Game.HudView>(true)).Single();
                var moves = (Component)new SerializedObject(hud).FindProperty("movesText").objectReferenceValue;
                Canvas counters = moves.GetComponentInParent<Canvas>(true);

                foreach (TMPro.TMP_Text text in hud.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    Assert.That(text.GetComponentInParent<Canvas>(true), Is.SameAs(counters), text.name);
                    Assert.That(text.font, Is.SameAs(((TMPro.TMP_Text)moves).font), text.name);
                }
            });
        }

        // Design §8.3 slice 5: one settings modal on every screen, over everything but the pause modal (it never shows with
        // it), so it covers the result card too; it starts closed, and its scene's entry opens it.
        [TestCaseSource(nameof(UiScenes))]
        public void EveryScreen_HasOneSettingsModal_AboveEveryCanvasButThePauseModal(SceneId scene)
        {
            WithScene(scene, roots =>
            {
                Game.SettingsModal settings = roots.SelectMany(root => root.GetComponentsInChildren<Game.SettingsModal>(true)).Single();
                Canvas own = settings.GetComponentInParent<Canvas>(true).rootCanvas;
                Assert.That(settings.gameObject.activeSelf, Is.False, "Starts closed.");

                var others = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                    .Where(canvas => canvas.isRootCanvas && canvas != own && canvas.GetComponentInChildren<Game.PauseOverlay>(true) == null);
                foreach (Canvas other in others)
                    Assert.That(own.sortingOrder, Is.GreaterThan(other.sortingOrder), other.name);

                ISceneEntry entry = roots.SelectMany(root => root.GetComponentsInChildren<ISceneEntry>(true)).Single();
                Assert.That(new SerializedObject((MonoBehaviour)entry).FindProperty("settings").objectReferenceValue, Is.SameAs(settings));
            });
        }

        // Decided 2026-10-25 (slice 4): the brief's Resume and Quit, plus Restart; Quit leads to level select.
        [Test]
        public void ThePauseModal_OffersResumeRestartAndQuit()
        {
            WithScene(SceneId.Game, roots =>
            {
                Game.PauseOverlay overlay = roots.SelectMany(root => root.GetComponentsInChildren<Game.PauseOverlay>(true)).Single();
                var serialized = new SerializedObject(overlay);
                string Label(string button) =>
                    ((Component)serialized.FindProperty(button).objectReferenceValue).GetComponentInChildren<TMPro.TMP_Text>(true).text;

                Assert.That(new[] { Label("resume"), Label("restart"), Label("quit") }, Is.EqualTo(new[] { "Resume", "Restart", "Quit" }));
                Assert.That(overlay.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, Is.EqualTo(3));
            });
        }

        static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // ADR-0012 rules 1 and 3: a counter changing rebuilds only its own small canvas, with no layout group to recompute.
        [Test]
        public void TheHudCounters_LiveOnTheirOwnCanvas_WithoutLayoutGroups()
        {
            WithScene(SceneId.Game, roots =>
            {
                Game.HudView hud = roots.SelectMany(root => root.GetComponentsInChildren<Game.HudView>(true)).Single();
                var serialized = new SerializedObject(hud);
                var moves = (Component)serialized.FindProperty("movesText").objectReferenceValue;
                var slots = (Component)serialized.FindProperty("slotsRoot").objectReferenceValue;

                Canvas countersCanvas = moves.GetComponentInParent<Canvas>(true);
                Assert.That(countersCanvas.isRootCanvas, Is.False, "Counters sit on a nested canvas.");
                Assert.That(slots.GetComponentInParent<Canvas>(true), Is.SameAs(countersCanvas));
                Assert.That(countersCanvas.GetComponentsInChildren<UnityEngine.UI.LayoutGroup>(true), Is.Empty);
            });
        }

        // ADR-0012 rules 4 and 8.
        [TestCaseSource(nameof(UiScenes))]
        public void EveryCanvas_ScalesAlike_AndFitsTheSafeArea(SceneId scene)
        {
            WithScene(scene, roots =>
            {
                var canvases = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Where(canvas => canvas.isRootCanvas).ToArray();
                Assert.That(canvases, Is.Not.Empty);
                foreach (Canvas canvas in canvases)
                {
                    var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                    Assert.That(scaler.uiScaleMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize), canvas.name);
                    Assert.That(scaler.screenMatchMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand), canvas.name);
                    Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080, 1920)), canvas.name);

                    var fitted = canvas.transform.Cast<Transform>().Where(child => child.GetComponent<Game.SafeAreaFitter>() != null).ToArray();
                    Assert.That(fitted.Length, Is.EqualTo(1), $"{canvas.name} has one safe-area root.");
                    Assert.That(canvas.transform.childCount, Is.EqualTo(1), $"Everything on {canvas.name} lives inside its safe-area root.");
                }
            });
        }

        // ADR-0012 refinement (Milestone 3 slice 4): all levels on one portrait screen, three across, no scrolling.
        [Test]
        public void LevelSelect_LaysLevelsOutThreeAcross_WithoutScrolling()
        {
            WithScene(SceneId.LevelSelect, roots =>
            {
                Game.LevelSelectView view = roots.SelectMany(root => root.GetComponentsInChildren<Game.LevelSelectView>(true)).Single();
                var grid = (RectTransform)new SerializedObject(view).FindProperty("grid").objectReferenceValue;
                var layout = grid.GetComponent<UnityEngine.UI.GridLayoutGroup>();

                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.constraint, Is.EqualTo(UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(layout.constraintCount, Is.EqualTo(3));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true)), Is.Empty);
            });
        }

        // Design §8.3 slice 6: the slowly breathing mushroom is Play, and it changes every frame, so it has its own canvas
        // (ADR-0012 rule 1) and the rest of the menu is never rebuilt for it.
        [Test]
        public void MainMenu_PlayIsTheBreathingMushroom_OnItsOwnCanvas()
        {
            WithScene(SceneId.MainMenu, roots =>
            {
                Game.MainMenuView view = roots.SelectMany(root => root.GetComponentsInChildren<Game.MainMenuView>(true)).Single();
                var play = (UnityEngine.UI.Button)new SerializedObject(view).FindProperty("play").objectReferenceValue;
                var breathing = play.GetComponent<Game.Breathing>();

                Assert.That(breathing, Is.Not.Null);
                Assert.That(breathing.Period, Is.InRange(2f, 6f), "A slow breath, seconds per breath.");
                Assert.That(breathing.Amount, Is.InRange(0.02f, 0.1f), "A gentle one, never a bounce.");
                Assert.That(play.GetComponentsInChildren<UnityEngine.UI.Image>(true).Select(image => image.sprite.name),
                    Is.EquivalentTo(new[] { "mushroom-cap", "mushroom-stem" }));
                Assert.That(play.GetComponentsInChildren<TMPro.TMP_Text>(true), Is.Empty, "No label: the mushroom is the button.");
                Canvas own = play.GetComponentInParent<Canvas>(true);
                Assert.That(own.isRootCanvas, Is.False, "It breathes on a canvas nested in the menu's.");
                Assert.That(own.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(graphic => graphic.transform.IsChildOf(play.transform)),
                    "Only the mushroom changes on that canvas.");
            });
        }

        // Design §8.3 slice 6: the mushroom is the only Play control, and settings sit alone in a footer at the bottom centre.
        [Test]
        public void MainMenu_HasOnlyTheMushroom_AndSettingsInTheFooter()
        {
            WithScene(SceneId.MainMenu, roots =>
            {
                Game.MainMenuView view = roots.SelectMany(root => root.GetComponentsInChildren<Game.MainMenuView>(true)).Single();
                var serialized = new SerializedObject(view);
                var play = (UnityEngine.UI.Button)serialized.FindProperty("play").objectReferenceValue;
                var settings = (UnityEngine.UI.Button)serialized.FindProperty("settings").objectReferenceValue;
                var menuButtons = roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    .Where(button => button.GetComponentInParent<Game.SettingsModal>(true) == null);

                Assert.That(menuButtons, Is.EquivalentTo(new[] { play, settings }));
                AssertInFooter((RectTransform)settings.transform, x: 0f);
            });
        }

        // Design §8.3 slice 6: help (?) and settings side by side, centred, in a footer under the levels. Help does
        // nothing until onboarding exists (slice 8; Soni: development builds only until then).
        [Test]
        public void LevelSelect_HasHelpAndSettings_SideBySideInTheFooter()
        {
            WithScene(SceneId.LevelSelect, roots =>
            {
                Game.LevelSelectView view = roots.SelectMany(root => root.GetComponentsInChildren<Game.LevelSelectView>(true)).Single();
                var serialized = new SerializedObject(view);
                var settings = (RectTransform)((UnityEngine.UI.Button)serialized.FindProperty("settings").objectReferenceValue).transform;
                var footer = roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    .Where(button => button.GetComponentInParent<Game.SettingsModal>(true) == null && button.GetComponent<Game.LevelButton>() == null)
                    .ToArray();
                var help = (RectTransform)footer.Single(button => button.image.sprite.name == "icon-help").transform;

                Assert.That(footer.Select(button => button.transform), Is.EquivalentTo(new Transform[] { help, settings }));
                Assert.That(settings.GetComponent<UnityEngine.UI.Image>().sprite.name, Is.EqualTo("icon-settings"));
                AssertInFooter(help, x: help.anchoredPosition.x);
                AssertInFooter(settings, x: -help.anchoredPosition.x);
                Assert.That(help.anchoredPosition.x, Is.LessThan(0f), "Help on the left.");
                Assert.That(help.anchoredPosition.y, Is.EqualTo(settings.anchoredPosition.y));
            });
        }

        // Anchored to the bottom centre of the safe area, at the given distance from the centre line.
        static void AssertInFooter(RectTransform button, float x)
        {
            Assert.That(button.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)), button.name);
            Assert.That(button.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)), button.name);
            Assert.That(button.anchoredPosition.x, Is.EqualTo(x).Within(0.01f), button.name);
            Assert.That(button.parent.GetComponent<Game.SafeAreaFitter>() != null || button.parent.GetComponentInParent<Game.SafeAreaFitter>() != null, button.name);
        }

        // Design §6: the pause overlay covers the board, the HUD and the result panel.
        [Test]
        public void ThePauseOverlay_DrawsAboveEveryOtherCanvas()
        {
            WithScene(SceneId.Game, roots =>
            {
                Game.PauseOverlay overlay = roots.SelectMany(root => root.GetComponentsInChildren<Game.PauseOverlay>(true)).Single();
                Canvas pauseCanvas = overlay.GetComponentInParent<Canvas>(true);
                Assert.That(pauseCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));

                var others = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                    .Where(canvas => canvas.isRootCanvas && canvas != pauseCanvas);
                foreach (Canvas other in others)
                {
                    Assert.That(other.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay), other.name);
                    Assert.That(pauseCanvas.sortingOrder, Is.GreaterThan(other.sortingOrder), other.name);
                }
            });
        }

        // Design §8.3: one font family for every text.
        [TestCaseSource(nameof(UiScenes))]
        public void EveryText_UsesTheGameFont_AndCanDrawItsText(SceneId scene)
        {
            WithScene(scene, roots =>
            {
                foreach (TMPro.TMP_Text text in roots.SelectMany(root => root.GetComponentsInChildren<TMPro.TMP_Text>(true)))
                {
                    Assert.That(ProjectFontTests.IsGameFont(text.font), Is.True, $"{text.name} uses {(text.font != null ? text.font.name : "no font")}.");
                    Assert.That(ProjectFontTests.Missing(text.font, text.text), Is.Empty, text.name);
                }
            });
        }

        // Design §8.3: every screen shows the gradient and the drifting spores, filling the camera that draws the screen.
        [TestCaseSource(nameof(UiScenes))]
        public void EveryScreen_HasOneBackdrop_UnderTheCameraItFills(SceneId scene)
        {
            WithScene(scene, roots =>
            {
                Game.Backdrop[] backdrops = roots.SelectMany(root => root.GetComponentsInChildren<Game.Backdrop>(true)).ToArray();
                Assert.That(backdrops.Length, Is.EqualTo(1));
                var viewCamera = (Camera)new SerializedObject(backdrops[0]).FindProperty("viewCamera").objectReferenceValue;
                Assert.That(backdrops[0].transform.parent, Is.SameAs(viewCamera.transform), "It moves with its camera.");

                Game.BoardCamera boardCamera = roots.SelectMany(root => root.GetComponentsInChildren<Game.BoardCamera>(true)).SingleOrDefault();
                if (boardCamera != null)
                    Assert.That(viewCamera, Is.SameAs(new SerializedObject(boardCamera).FindProperty("boardCamera").objectReferenceValue),
                        "In the game, it fills the camera fitted to the board.");
            });
        }

        // ADR-0012 rule 7: UI images draw from one UI atlas, so each screen batches.
        [TestCaseSource(nameof(UiScenes))]
        public void EveryUiImage_DrawsFromTheUiAtlas(SceneId scene)
        {
            var atlas = AssetDatabase.FindAssets("UIAtlas t:SpriteAtlas", new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single();

            WithScene(scene, roots =>
            {
                foreach (UnityEngine.UI.Image image in roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Image>(true)))
                {
                    Assert.That(image.sprite, Is.Not.Null, $"{image.name} has no sprite.");
                    Assert.That(atlas.CanBindTo(image.sprite), Is.True, $"{image.name}'s {image.sprite.name} is not in {atlas.name}.");
                }
            });
        }

        [TestCaseSource(nameof(UiScenes))]
        public void EveryUiScene_HasOneEventSystem_ReadingTheInputSystem(SceneId scene)
        {
            WithScene(scene, roots =>
            {
                var systems = roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).ToArray();
                Assert.That(systems.Length, Is.EqualTo(1));
                Assert.That(systems[0].GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(), Is.Not.Null);
            });
        }

        [Test]
        public void PressingPlayInASavedScene_BootsThroughBootstrap()
        {
            Assert.That(PlayModeStartScene.BootsThroughBootstrap("Assets/_Mycelia/Scenes/Game.unity"), Is.True);
        }

        [Test]
        public void PlayModeTests_KeepTheTestRunnersOwnScene()
        {
            // The Test Runner saves this scene and makes it active before entering Play (CreateBootstrapSceneTask).
            Assert.That(PlayModeStartScene.BootsThroughBootstrap("Assets/InitTestScene3f2a9c1e-4b7d-4e8a-9f0c-2d1b6a5e7c3f.unity"), Is.False);
        }

        [Test]
        public void PressingPlayInAnUntitledScene_PlaysItAsItIs()
        {
            Assert.That(PlayModeStartScene.BootsThroughBootstrap(""), Is.False);
        }
    }
}
