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
                Assert.That(serialized.FindProperty("startLevel").objectReferenceValue, Is.Not.Null, "Start Level");
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
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script")
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

        // ADR-0012 rule 2: the HUD must never swallow a touch meant for the board.
        [Test]
        public void OnlyButtons_TakeTouches()
        {
            WithScene(SceneId.Game, roots =>
            {
                var touchable = roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    .Where(graphic => graphic.raycastTarget && graphic.GetComponentInParent<UnityEngine.UI.Selectable>(true) == null);
                Assert.That(touchable.Select(graphic => graphic.name), Is.Empty);
            });
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
        [Test]
        public void EveryCanvas_ScalesAlike_AndFitsTheSafeArea()
        {
            WithScene(SceneId.Game, roots =>
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

        // ADR-0012 rule 7: UI images draw from one UI atlas, so the HUD batches.
        [Test]
        public void EveryUiImage_InTheGameScene_DrawsFromTheUiAtlas()
        {
            var atlas = AssetDatabase.FindAssets("UIAtlas t:SpriteAtlas", new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single();

            WithScene(SceneId.Game, roots =>
            {
                foreach (UnityEngine.UI.Image image in roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Image>(true)))
                {
                    Assert.That(image.sprite, Is.Not.Null, $"{image.name} has no sprite.");
                    Assert.That(atlas.CanBindTo(image.sprite), Is.True, $"{image.name}'s {image.sprite.name} is not in {atlas.name}.");
                }
            });
        }

        [Test]
        public void TheGameScene_HasOneEventSystem_ReadingTheInputSystem()
        {
            WithScene(SceneId.Game, roots =>
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
