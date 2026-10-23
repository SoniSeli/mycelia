using NUnit.Framework;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class SafeAreaFitterTests
    {
        [Test]
        public void WithoutNotchOrBar_TheUiFillsTheScreen()
        {
            (Vector2 min, Vector2 max) = SafeAreaFitter.AnchorsFor(new Rect(0, 0, 1080, 1920), new Vector2(1080, 1920));

            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void ANotchAndGestureBar_AreKeptClearOf()
        {
            // 20:9 phone: 100 px notch at the top, 48 px gesture bar at the bottom.
            (Vector2 min, Vector2 max) = SafeAreaFitter.AnchorsFor(new Rect(0, 48, 1080, 2252), new Vector2(1080, 2400));

            Assert.That(min.x, Is.EqualTo(0f));
            Assert.That(min.y, Is.EqualTo(48f / 2400).Within(1e-6f));
            Assert.That(max.x, Is.EqualTo(1f));
            Assert.That(max.y, Is.EqualTo(2300f / 2400).Within(1e-6f));
        }

        [Test]
        public void Fitting_SetsTheAnchors_WithNoOffsets()
        {
            var root = new GameObject("Safe Area", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.offsetMin = new Vector2(5, 5);
            rect.offsetMax = new Vector2(-5, -5);
            var fitter = root.AddComponent<SafeAreaFitter>();

            fitter.Fit(new Rect(0, 48, 1080, 2252), new Vector2(1080, 2400));

            Assert.That(rect.anchorMin.y, Is.EqualTo(48f / 2400).Within(1e-6f));
            Assert.That(rect.anchorMax.y, Is.EqualTo(2300f / 2400).Within(1e-6f));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
            Object.DestroyImmediate(root);
        }
    }
}
