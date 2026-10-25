using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mycelia.Content.Tests
{
    /// <summary>
    /// Guards the shipped spores' art (design §6 and §8.3, ADR-0005): circles of one medium size, told apart by their
    /// outlines, so no player has to tell them apart by colour. Reads the body of each sprite, not its faint glow.
    /// </summary>
    public class ProjectSporeArtTests
    {
        // The glow around a spore stays fainter than this; the body is opaque.
        const float BodyAlpha = 0.5f;

        // Design §8.3: bigger than the mockup's spores, smaller than the old ones (diameters in cells).
        const float MockupDiameter = 0.54f;
        const float OldDiameter = 0.84f;

        static Sprite[] SporeSprites =>
            AssetDatabase.FindAssets("t:SporeCatalog", new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<SporeCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single().Types.Select(type => type.Sprite).ToArray();

        /// <summary>The sprite's opaque pixels, read from its source file so import settings don't matter.</summary>
        static bool[,] Body(Sprite sprite)
        {
            var source = new Texture2D(2, 2);
            try
            {
                // The sprite's own file: sprite.texture can be the packed atlas in the Editor.
                string path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(source.LoadImage(File.ReadAllBytes(path)), Is.True, path);
                Rect rect = sprite.rect;
                var body = new bool[(int)rect.width, (int)rect.height];
                for (int y = 0; y < body.GetLength(1); y++)
                for (int x = 0; x < body.GetLength(0); x++)
                    body[x, y] = source.GetPixel((int)rect.x + x, (int)rect.y + y).a >= BodyAlpha;
                return body;
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        /// <summary>How far the body reaches from the sprite's pivot, in pixels.</summary>
        static float Radius(Sprite sprite, bool[,] body)
        {
            float radius = 0;
            for (int y = 0; y < body.GetLength(1); y++)
            for (int x = 0; x < body.GetLength(0); x++)
                if (body[x, y])
                    radius = Mathf.Max(radius, (new Vector2(x + 0.5f, y + 0.5f) - sprite.pivot).magnitude);
            return radius;
        }

        static float Diameter(Sprite sprite) => 2 * Radius(sprite, Body(sprite)) / sprite.pixelsPerUnit;

        [Test]
        public void EverySpore_IsMediumSized_BiggerThanTheMockupsAndSmallerThanTheOldOnes()
        {
            foreach (Sprite sprite in SporeSprites)
                Assert.That(Diameter(sprite), Is.GreaterThan(MockupDiameter).And.LessThan(OldDiameter), sprite.name);
        }

        // Bumps, spikes and bites take area away from a circle, so a smooth spore as wide as the others looks bigger
        // (seen on the phone, 2026-10-25): spores match in area, not in diameter.
        [Test]
        public void EverySpore_CoversAboutTheSameArea_SoNoTypeLooksBigger()
        {
            Sprite[] sprites = SporeSprites;
            float[] areas = sprites.Select(Area).ToArray();
            float mean = areas.Average();

            for (int i = 0; i < sprites.Length; i++)
                Assert.That(areas[i] / mean, Is.InRange(0.92f, 1.08f), $"{sprites[i].name}: areas {string.Join(", ", areas)}");
        }

        /// <summary>The body's area in square cells.</summary>
        static float Area(Sprite sprite)
        {
            bool[,] body = Body(sprite);
            int opaque = 0;
            foreach (bool pixel in body)
                if (pixel)
                    opaque++;
            return opaque / (sprite.pixelsPerUnit * sprite.pixelsPerUnit);
        }

        [Test]
        public void EverySpore_IsACircleAroundItsCellCentre_WithOnlyItsOutlineChanged()
        {
            foreach (Sprite sprite in SporeSprites)
            {
                bool[,] body = Body(sprite);
                float radius = Radius(sprite, body);
                int inside = 0, filled = 0;
                for (int y = 0; y < body.GetLength(1); y++)
                for (int x = 0; x < body.GetLength(0); x++)
                {
                    if ((new Vector2(x + 0.5f, y + 0.5f) - sprite.pivot).magnitude > radius)
                        continue;
                    inside++;
                    if (body[x, y])
                        filled++;
                }

                Assert.That(sprite.pivot, Is.EqualTo(sprite.rect.size / 2), $"{sprite.name} is centred on its cell.");
                Assert.That(filled / (float)inside, Is.GreaterThan(0.7f), $"{sprite.name} fills its circle.");
            }
        }

        [Test]
        public void EveryTwoSpores_HaveDifferentOutlines_NotOnlyDifferentColours()
        {
            Sprite[] sprites = SporeSprites;
            for (int i = 0; i < sprites.Length; i++)
            for (int j = i + 1; j < sprites.Length; j++)
            {
                bool[,] a = Body(sprites[i]), b = Body(sprites[j]);
                Assert.That(a.GetLength(0), Is.EqualTo(b.GetLength(0)), "Spore sprites share one canvas size.");
                Assert.That(a.GetLength(1), Is.EqualTo(b.GetLength(1)), "Spore sprites share one canvas size.");

                int either = 0, onlyOne = 0;
                for (int y = 0; y < a.GetLength(1); y++)
                for (int x = 0; x < a.GetLength(0); x++)
                {
                    if (a[x, y] || b[x, y])
                        either++;
                    if (a[x, y] != b[x, y])
                        onlyOne++;
                }

                Assert.That(onlyOne / (float)either, Is.GreaterThan(0.04f), $"{sprites[i].name} and {sprites[j].name} look alike in grayscale.");
            }
        }
    }
}
