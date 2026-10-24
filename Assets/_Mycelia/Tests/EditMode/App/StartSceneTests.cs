using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class StartSceneTests
    {
        [Test]
        public void ABuild_OpensTheMainMenu()
        {
            StartScene start = StartScene.Choose(editorScenePath: null);

            Assert.That(start.Scene, Is.EqualTo(SceneId.MainMenu));
            Assert.That(start.Args, Is.Null);
        }

        [Test]
        public void PressingPlayInBootstrap_OpensTheMainMenu()
        {
            StartScene start = StartScene.Choose("Assets/_Mycelia/Scenes/Bootstrap.unity");

            Assert.That(start.Scene, Is.EqualTo(SceneId.MainMenu));
        }

        [TestCase("Assets/_Mycelia/Scenes/Game.unity", SceneId.Game)]
        [TestCase("Assets/_Mycelia/Scenes/LevelSelect.unity", SceneId.LevelSelect)]
        public void PressingPlayInAnAppScene_ReopensItWithItsDebugArgs(string path, SceneId scene)
        {
            StartScene start = StartScene.Choose(path);

            Assert.That(start.Scene, Is.EqualTo(scene));
            Assert.That(start.Args, Is.Null, "No args, so the scene uses its own debug args.");
        }

        [Test]
        public void PressingPlayInASceneOutsideTheApp_OpensTheMainMenu()
        {
            StartScene start = StartScene.Choose("Assets/Sandbox/Game Sketch.unity");

            Assert.That(start.Scene, Is.EqualTo(SceneId.MainMenu));
        }
    }
}
