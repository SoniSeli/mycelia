using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    public class GradientGraphicTests
    {
        static readonly Color Top = new Color32(6, 54, 149, 255);
        static readonly Color Bottom = new Color32(66, 186, 190, 255);

        GameObject root;
        GradientGraphic gradient;

        [SetUp]
        public void CreateGradient()
        {
            root = new GameObject("Gradient", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 2400);
            gradient = root.AddComponent<GradientGraphic>();
            var serialized = new SerializedObject(gradient);
            serialized.FindProperty("top").colorValue = Top;
            serialized.FindProperty("bottom").colorValue = Bottom;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void DestroyGradient() => Object.DestroyImmediate(root);

        List<UIVertex> Vertices()
        {
            var helper = new VertexHelper();
            typeof(Graphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                .Invoke(gradient, new object[] { helper });
            var vertices = new List<UIVertex>();
            helper.GetUIVertexStream(vertices);
            return vertices;
        }

        [Test]
        public void TheGradient_FillsItsWholeRect()
        {
            List<UIVertex> vertices = Vertices();
            Rect rect = gradient.rectTransform.rect;

            Assert.That(vertices.Min(v => v.position.x), Is.EqualTo(rect.xMin));
            Assert.That(vertices.Max(v => v.position.x), Is.EqualTo(rect.xMax));
            Assert.That(vertices.Min(v => v.position.y), Is.EqualTo(rect.yMin));
            Assert.That(vertices.Max(v => v.position.y), Is.EqualTo(rect.yMax));
        }

        [Test]
        public void TheGradient_RunsFromTheTopColour_DownToTheBottomColour()
        {
            List<UIVertex> vertices = Vertices();
            float top = vertices.Max(v => v.position.y), bottom = vertices.Min(v => v.position.y);

            foreach (UIVertex vertex in vertices.Where(v => Mathf.Approximately(v.position.y, top)))
                Assert.That(vertex.color, Is.EqualTo((Color32)Top), "top");
            foreach (UIVertex vertex in vertices.Where(v => Mathf.Approximately(v.position.y, bottom)))
                Assert.That(vertex.color, Is.EqualTo((Color32)Bottom), "bottom");
        }

        // The same rows, blended the same way, as the backdrop: a fade over a screen looks like its background.
        [Test]
        public void TheGradient_IsBlendedInTheBackdropsRows()
        {
            List<UIVertex> vertices = Vertices();
            float[] rows = vertices.Select(v => v.position.y).Distinct().OrderBy(y => y).ToArray();
            Assert.That(rows.Length, Is.EqualTo(Backdrop.GradientRows));

            UIVertex middle = vertices.First(v => Mathf.Approximately(v.position.y, rows[rows.Length / 2]));
            Assert.That(middle.color, Is.EqualTo((Color32)Color.Lerp(Bottom, Top, 0.5f)));
        }
    }
}
