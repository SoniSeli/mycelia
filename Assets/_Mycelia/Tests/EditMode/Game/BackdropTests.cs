using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.Tests
{
    public class BackdropTests
    {
        static readonly Color Top = new Color(0.02f, 0.21f, 0.58f);
        static readonly Color Bottom = new Color(0.26f, 0.73f, 0.75f);

        GameObject cameraObject;
        Camera camera;
        Backdrop backdrop;
        ParticleSystem spores;

        [SetUp]
        public void MakeBackdrop()
        {
            cameraObject = new GameObject("Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 1080f / 2400f;
            cameraObject.transform.position = new Vector3(3, 4, -10);

            var backdropObject = new GameObject("Backdrop", typeof(MeshFilter), typeof(MeshRenderer));
            backdropObject.transform.SetParent(cameraObject.transform, false);
            backdropObject.transform.localPosition = new Vector3(0, 0, 5);
            spores = new GameObject("Spores").AddComponent<ParticleSystem>();
            spores.transform.SetParent(backdropObject.transform, false);
            backdrop = backdropObject.AddComponent<Backdrop>();

            var serialized = new SerializedObject(backdrop);
            serialized.FindProperty("viewCamera").objectReferenceValue = camera;
            serialized.FindProperty("gradient").objectReferenceValue = backdropObject.GetComponent<MeshFilter>();
            serialized.FindProperty("spores").objectReferenceValue = spores;
            serialized.FindProperty("top").colorValue = Top;
            serialized.FindProperty("bottom").colorValue = Bottom;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void DestroyBackdrop() => Object.DestroyImmediate(cameraObject);

        Vector3[] WorldVertices()
        {
            Mesh mesh = backdrop.GetComponent<MeshFilter>().sharedMesh;
            return mesh.vertices.Select(vertex => backdrop.transform.TransformPoint(vertex)).ToArray();
        }

        static void AssertCovers(Vector3[] vertices, Camera camera)
        {
            float halfHeight = camera.orthographicSize, halfWidth = halfHeight * camera.aspect;
            Vector3 centre = camera.transform.position;
            Assert.That(vertices.Min(v => v.x), Is.AtMost(centre.x - halfWidth), "left edge");
            Assert.That(vertices.Max(v => v.x), Is.AtLeast(centre.x + halfWidth), "right edge");
            Assert.That(vertices.Min(v => v.y), Is.AtMost(centre.y - halfHeight), "bottom edge");
            Assert.That(vertices.Max(v => v.y), Is.AtLeast(centre.y + halfHeight), "top edge");
        }

        static Color InColourSpace(Color colour) => QualitySettings.activeColorSpace == ColorSpace.Linear ? colour.linear : colour;

        [Test]
        public void TheGradient_CoversTheCamerasWholeView()
        {
            backdrop.FitIfChanged();

            AssertCovers(WorldVertices(), camera);
        }

        [Test]
        public void TheGradient_RunsFromTheTopColour_DownToTheBottomColour()
        {
            backdrop.FitIfChanged();

            Mesh mesh = backdrop.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Color[] colours = mesh.colors;
            float topY = vertices.Max(v => v.y), bottomY = vertices.Min(v => v.y);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Mathf.Approximately(vertices[i].y, topY))
                    Assert.That(colours[i], Is.EqualTo(InColourSpace(Top)).Using(ColorEqualityComparer.Instance), "top");
                if (Mathf.Approximately(vertices[i].y, bottomY))
                    Assert.That(colours[i], Is.EqualTo(InColourSpace(Bottom)).Using(ColorEqualityComparer.Instance), "bottom");
            }
        }

        [Test]
        public void TheGradientsMiddle_IsBlendedLikeTheReference_NotInLinearLight()
        {
            backdrop.FitIfChanged();

            Mesh mesh = backdrop.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int middle = System.Array.FindIndex(vertices, v => Mathf.Abs(v.y) < 0.001f);
            Assert.That(middle, Is.Not.EqualTo(-1), "The gradient has a row through the middle.");
            Assert.That(mesh.colors[middle], Is.EqualTo(InColourSpace(Color.Lerp(Bottom, Top, 0.5f))).Using(ColorEqualityComparer.Instance));
        }

        // The gradient samples the spore texture's opaque white core, so the gradient and the spores need only one material.
        [Test]
        public void TheGradient_SamplesTheCentreOfItsTexture()
        {
            backdrop.FitIfChanged();

            Vector2[] uvs = backdrop.GetComponent<MeshFilter>().sharedMesh.uv;
            Assert.That(uvs, Is.Not.Empty);
            Assert.That(uvs.Distinct(), Is.EqualTo(new[] { new Vector2(0.5f, 0.5f) }));
        }

        [Test]
        public void WhenTheCameraChanges_TheBackdropRefits_AndTheSporesScaleWithTheView()
        {
            backdrop.FitIfChanged();
            camera.orthographicSize = 8;
            camera.aspect = 0.75f;

            backdrop.FitIfChanged();

            AssertCovers(WorldVertices(), camera);
            float scale = 8 / Backdrop.ReferenceSize;
            Assert.That(spores.transform.localScale, Is.EqualTo(new Vector3(scale, scale, 1)));
            Vector3 box = spores.shape.scale;
            Assert.That(box.x * scale, Is.AtLeast(2 * 8 * 0.75f), "The spores are spread across the view's width.");
            Assert.That(box.y * scale, Is.AtLeast(2 * 8f), "... and its height.");
        }

        [Test]
        public void CheckingForChanges_WhenNothingChanged_AllocatesNothing()
        {
            backdrop.FitIfChanged();

            Assert.That(() => backdrop.FitIfChanged(), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TheBackdrop_DrawsBehindTheBoard_AndTheSporesBetween()
        {
            backdrop.FitIfChanged();

            Assert.That(Backdrop.GradientSortingOrder, Is.LessThan(Backdrop.SporeSortingOrder));
            Assert.That(Backdrop.SporeSortingOrder, Is.LessThan(BoardView.CellSortingOrder));
            Assert.That(backdrop.GetComponent<MeshRenderer>().sortingOrder, Is.EqualTo(Backdrop.GradientSortingOrder));
            Assert.That(spores.GetComponent<ParticleSystemRenderer>().sortingOrder, Is.EqualTo(Backdrop.SporeSortingOrder));
        }

        // ADR-0013: anything that animates stops under the pause overlay.
        [Test]
        public void Pausing_FreezesTheSpores_AndResumingLetsThemDriftAgain()
        {
            spores.Play();

            backdrop.SetPaused(true);
            Assert.That(spores.isPaused, Is.True);

            backdrop.SetPaused(false);
            Assert.That(spores.isPlaying, Is.True);
        }
    }

    sealed class ColorEqualityComparer : System.Collections.Generic.IEqualityComparer<Color>
    {
        public static readonly ColorEqualityComparer Instance = new ColorEqualityComparer();

        public bool Equals(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f && Mathf.Abs(a.a - b.a) < 0.002f;

        public int GetHashCode(Color colour) => 0;
    }
}
