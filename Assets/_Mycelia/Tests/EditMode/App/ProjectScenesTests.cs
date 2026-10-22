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
