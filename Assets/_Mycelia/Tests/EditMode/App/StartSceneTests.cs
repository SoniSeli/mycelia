using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class StartSceneTests
    {
        GameSceneArgs startLevel;

        [SetUp]
        public void CreateArgs() => startLevel = new GameSceneArgs(ProjectAssets.TestLevel);

        [Test]
        public void ABuild_StartsTheGameWithTheStartLevel()
        {
            StartScene start = StartScene.Choose(startLevel, editorScenePath: null);

            Assert.That(start.Scene, Is.EqualTo(SceneId.Game));
            Assert.That(start.Args, Is.SameAs(startLevel));
        }

        [Test]
        public void PressingPlayInBootstrap_StartsTheGameWithTheStartLevel()
        {
            StartScene start = StartScene.Choose(startLevel, "Assets/_Mycelia/Scenes/Bootstrap.unity");

            Assert.That(start.Scene, Is.EqualTo(SceneId.Game));
            Assert.That(start.Args, Is.SameAs(startLevel));
        }

        [Test]
        public void PressingPlayInAnAppScene_ReopensItWithItsDebugArgs()
        {
            StartScene start = StartScene.Choose(startLevel, "Assets/_Mycelia/Scenes/Game.unity");

            Assert.That(start.Scene, Is.EqualTo(SceneId.Game));
            Assert.That(start.Args, Is.Null, "No args, so the scene uses its own debug args.");
        }

        [Test]
        public void PressingPlayInASceneOutsideTheApp_StartsTheGameWithTheStartLevel()
        {
            StartScene start = StartScene.Choose(startLevel, "Assets/Sandbox/Game Sketch.unity");

            Assert.That(start.Scene, Is.EqualTo(SceneId.Game));
            Assert.That(start.Args, Is.SameAs(startLevel));
        }
    }
}
