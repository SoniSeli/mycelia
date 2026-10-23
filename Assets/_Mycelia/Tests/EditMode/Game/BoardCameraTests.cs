using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class BoardCameraTests
    {
        GameObject cameraObject;
        RenderTexture target;

        [TearDown]
        public void DestroyCamera()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void TheCamera_ShowsEachCellWhereTheLayoutPutsIt()
        {
            var screen = new Vector2(1080, 2400);
            target = new RenderTexture((int)screen.x, (int)screen.y, 0);
            cameraObject = new GameObject("Board Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.targetTexture = target;
            var boardCamera = cameraObject.AddComponent<BoardCamera>();
            var serialized = new SerializedObject(boardCamera);
            serialized.FindProperty("boardCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            BoardLayout layout = BoardLayout.Fit(6, 6, screen, new Rect(0, 48, 1080, 2252), new BoardInsets(0.2f, 0.08f, 0.04f));
            boardCamera.Apply(layout);

            foreach (Vector2 world in new[] { Vector2.zero, new Vector2(6, 6), new Vector2(2.5f, 4.5f) })
            {
                Vector3 actual = camera.WorldToScreenPoint(world);
                Vector2 expected = layout.WorldToScreen(world);
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.5f), $"{world} x");
                Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.5f), $"{world} y");
            }
        }
    }
}
