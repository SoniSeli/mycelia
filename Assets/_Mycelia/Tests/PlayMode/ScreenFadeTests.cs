using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mycelia.Game.PlayTests
{
    public class ScreenFadeTests
    {
        const float Duration = 0.1f;

        GameObject root;
        Canvas canvas;
        CanvasGroup group;
        ScreenFade fade;

        // As in the Bootstrap scene: covered from the start, so the first screen fades in too.
        [SetUp]
        public void CreateFade()
        {
            root = new GameObject("Screen Fade", typeof(Canvas), typeof(CanvasGroup));
            canvas = root.GetComponent<Canvas>();
            group = root.GetComponent<CanvasGroup>();
            group.alpha = 1;
            group.blocksRaycasts = true;
            fade = root.AddComponent<ScreenFade>();
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(fade);
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("group").objectReferenceValue = group;
            serialized.FindProperty("duration").floatValue = Duration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        [TearDown]
        public void DestroyFade() => Object.Destroy(root);

        static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 2f)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < until)
                yield return null;
        }

        [UnityTest]
        public IEnumerator FadingIn_ClearsTheScreen_ThenLetsTapsThrough_AndSwitchesItsCanvasOff()
        {
            bool shown = false;
            fade.FadeIn(() => shown = true);
            Assert.That(group.blocksRaycasts, Is.True, "Taps are caught while it fades.");

            yield return WaitFor(() => shown);

            Assert.That(shown, Is.True);
            Assert.That(group.alpha, Is.EqualTo(0f));
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(canvas.enabled, Is.False, "A clear fade costs nothing to draw.");
        }

        [UnityTest]
        public IEnumerator FadingOut_CoversTheScreenFully_BeforeCallingBack()
        {
            fade.FadeIn(() => { });
            yield return WaitFor(() => !canvas.enabled);

            float alphaWhenCovered = -1;
            fade.FadeOut(() => alphaWhenCovered = group.alpha);
            Assert.That(canvas.enabled, Is.True);
            Assert.That(group.blocksRaycasts, Is.True, "Taps are caught from the start of the fade.");
            Assert.That(alphaWhenCovered, Is.EqualTo(-1f), "Not covered yet.");

            yield return WaitFor(() => alphaWhenCovered >= 0);

            Assert.That(alphaWhenCovered, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator FadingOut_TakesAboutItsDuration()
        {
            fade.FadeIn(() => { });
            yield return WaitFor(() => !canvas.enabled);

            float started = Time.time, covered = -1;
            fade.FadeOut(() => covered = Time.time);
            yield return WaitFor(() => covered >= 0);

            Assert.That(covered - started, Is.EqualTo(Duration).Within(0.06f));
        }

        // The first scene is requested while the screen is still covered from launch: no wait.
        [Test]
        public void FadingOut_WhenAlreadyCovered_CallsBackAtOnce()
        {
            bool covered = false;

            fade.FadeOut(() => covered = true);

            Assert.That(covered, Is.True);
        }
    }
}
