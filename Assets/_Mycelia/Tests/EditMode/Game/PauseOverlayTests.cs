using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    public class PauseOverlayTests
    {
        GameObject root;
        PauseOverlay overlay;
        Button resume;
        Button restart;
        Button quit;
        int resumeClicks;
        int restartClicks;
        int quitClicks;

        [SetUp]
        public void CreateOverlay()
        {
            resumeClicks = restartClicks = quitClicks = 0;
            root = new GameObject("Pause Panel", typeof(RectTransform));
            overlay = root.AddComponent<PauseOverlay>();
            resume = Child("Resume").AddComponent<Button>();
            restart = Child("Restart").AddComponent<Button>();
            quit = Child("Quit").AddComponent<Button>();
            TestHud.Set(overlay, "resume", resume);
            TestHud.Set(overlay, "restart", restart);
            TestHud.Set(overlay, "quit", quit);
            overlay.Init(() => resumeClicks++, () => restartClicks++, () => quitClicks++);
        }

        [TearDown]
        public void DestroyOverlay() => Object.DestroyImmediate(root);

        GameObject Child(string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child;
        }

        [Test]
        public void TheOverlay_StartsHidden()
        {
            Assert.That(root.activeSelf, Is.False);
        }

        [Test]
        public void TheOverlay_ShowsWhilePaused_AndHidesOnResume()
        {
            overlay.SetPaused(true);
            Assert.That(root.activeSelf, Is.True);

            overlay.SetPaused(false);
            Assert.That(root.activeSelf, Is.False);
        }

        [Test]
        public void Resume_AsksToResumeTheLevel()
        {
            overlay.SetPaused(true);

            resume.onClick.Invoke();

            Assert.That(resumeClicks, Is.EqualTo(1));
            Assert.That(restartClicks, Is.Zero);
        }

        [Test]
        public void Restart_AsksToRestartTheLevel()
        {
            overlay.SetPaused(true);

            restart.onClick.Invoke();

            Assert.That(restartClicks, Is.EqualTo(1));
            Assert.That(resumeClicks, Is.Zero);
        }

        [Test]
        public void Quit_AsksToLeaveTheLevel()
        {
            overlay.SetPaused(true);

            quit.onClick.Invoke();

            Assert.That(quitClicks, Is.EqualTo(1));
            Assert.That(resumeClicks + restartClicks, Is.Zero);
        }
    }
}
