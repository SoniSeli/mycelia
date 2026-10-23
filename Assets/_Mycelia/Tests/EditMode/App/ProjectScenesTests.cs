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

        [Test]
        public void PressingPlayInASavedScene_BootsThroughBootstrap()
        {
            Assert.That(PlayModeStartScene.BootsThroughBootstrap("Assets/_Mycelia/Scenes/Game.unity"), Is.True);
        }

        [Test]
        public void PressingPlayInAnUntitledScene_PlaysItAsItIs()
        {
            Assert.That(PlayModeStartScene.BootsThroughBootstrap(""), Is.False);
        }
    }
}
