using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Mycelia.Game.Tests
{
    /// <summary>Guards the real prefabs and atlases (ADR-0002, ADR-0011, ADR-0012).</summary>
    public class ProjectArtTests
    {
        // Only the project's own assets: packages ship atlases and prefabs too, and whether they show up depends on the Library.
        static T[] LoadAll<T>(string filter) where T : Object =>
            AssetDatabase.FindAssets(filter, new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

        static SpriteRenderer SporePrefab =>
            LoadAll<GameObject>("t:Prefab").Select(prefab => prefab.GetComponent<SporeView>()).Single(view => view != null).Renderer;

        // Design §8.3 slice 6: each level is one of Soni's chunky nodes, with soft stars and the lock icon.
        [Test]
        public void TheLevelButton_IsSonisChunkyNode_WithSoftStarsAndTheLock()
        {
            LevelButton button = LoadAll<GameObject>("t:Prefab").Select(prefab => prefab.GetComponent<LevelButton>()).Single(b => b != null);
            var serialized = new SerializedObject(button);
            var node = (UnityEngine.UI.Image)serialized.FindProperty("node").objectReferenceValue;
            var lockIcon = (UnityEngine.UI.Image)serialized.FindProperty("lockIcon").objectReferenceValue;
            SerializedProperty stars = serialized.FindProperty("stars");

            Assert.That(node.sprite.name, Is.EqualTo("chunky"));
            Assert.That(node.preserveAspect, Is.True, "The shape is drawn, not stretched.");
            Assert.That(lockIcon.sprite.name, Is.EqualTo("lock"));
            Assert.That(stars.arraySize, Is.EqualTo(3));
            for (int i = 0; i < stars.arraySize; i++)
                Assert.That(((UnityEngine.UI.Image)stars.GetArrayElementAtIndex(i).objectReferenceValue).sprite.name, Is.EqualTo("star-soft"));

            // Soni: the number, the row of stars and the lock sit centred on the node.
            var number = (RectTransform)((Component)serialized.FindProperty("number").objectReferenceValue).transform;
            float[] starX = Enumerable.Range(0, 3).Select(i => ((Component)stars.GetArrayElementAtIndex(i).objectReferenceValue).GetComponent<RectTransform>().anchoredPosition.x).ToArray();
            Assert.That(number.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f), "Number");
            Assert.That(starX.Sum(), Is.EqualTo(0f).Within(0.01f), "Stars");
            Assert.That(starX[1], Is.EqualTo(0f).Within(0.01f), "Middle star");
            Assert.That(lockIcon.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero), "Lock");
            Assert.That(button.GetComponent<UnityEngine.UI.Button>().colors.disabledColor, Is.EqualTo(Color.white),
                "The node's own colour dims a locked level, not the button's tint on top of it.");
        }

        // Design §8.3 slice 6 (Soni): the stars have a dark outline, so they read on the light node; the fill stays white
        // to take the UI's tint.
        [Test]
        public void TheStar_HasADarkOutline_AroundAWhiteFill()
        {
            var texture = new Texture2D(2, 2);
            Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes("Assets/_Mycelia/Art/UI/star-soft.png")), Is.True);
            int middle = texture.width / 2;
            // Up the middle column from the centre: white, then the outline's opaque dark pixels before the edge.
            Color[] column = Enumerable.Range(texture.height / 2, texture.height / 2).Select(y => texture.GetPixel(middle, y)).ToArray();
            Color[] opaque = column.Where(pixel => pixel.a > 0.9f).ToArray();

            Assert.That(opaque.First().grayscale, Is.GreaterThan(0.95f), "White at the centre.");
            Assert.That(opaque.Last().grayscale, Is.LessThan(0.3f), "Dark at the rim.");
            Object.DestroyImmediate(texture);
        }

        // Design §8.3 slice 6 (Soni): the lock's shackle ends inside its body, with no bumps poking out underneath.
        [Test]
        public void TheLock_HasAFlatBottom_WithNothingPokingOutUnderItsBody()
        {
            var texture = new Texture2D(2, 2);
            Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes("Assets/_Mycelia/Art/UI/lock.png")), Is.True);
            int bottom = Enumerable.Range(0, texture.height).First(y => Enumerable.Range(0, texture.width).Any(x => texture.GetPixel(x, y).a > 0.5f));

            // Count separate opaque runs in the lowest few rows: the body's bottom is one run; bumps would be two.
            for (int y = bottom; y < bottom + 4; y++)
            {
                int runs = 0;
                bool inside = false;
                for (int x = 0; x < texture.width; x++)
                {
                    bool opaque = texture.GetPixel(x, y).a > 0.5f;
                    if (opaque && !inside) runs++;
                    inside = opaque;
                }
                Assert.That(runs, Is.EqualTo(1), $"Row {y} from the bottom.");
            }
            Object.DestroyImmediate(texture);
        }

        // Design §8.3 took the cell tiles away: the board atlas holds the spores and nothing else.
        [Test]
        public void TheBoardAtlas_PacksEverySporeSprite_AndNothingElse()
        {
            SpriteAtlas atlas = LoadAll<SpriteAtlas>("BoardAtlas t:SpriteAtlas").Single();
            Sprite[] spores = TestViews.ProjectSpores.Types.Select(type => type.Sprite).ToArray();

            foreach (Sprite sprite in spores)
            {
                Assert.That(sprite, Is.Not.Null);
                Assert.That(atlas.CanBindTo(sprite), Is.True, $"{sprite.name} is not in {atlas.name}.");
            }
            Assert.That(PackedTextures(atlas), Is.EquivalentTo(spores.Select(AssetDatabase.GetAssetPath).Distinct()));
        }

        static string[] PackedTextures(SpriteAtlas atlas)
        {
            Object[] packables = atlas.GetPackables();
            string[] folders = packables.Select(AssetDatabase.GetAssetPath).Where(AssetDatabase.IsValidFolder).ToArray();
            return AssetDatabase.FindAssets("t:Texture2D", folders).Select(AssetDatabase.GUIDToAssetPath)
                .Concat(packables.Where(packable => !(packable is DefaultAsset)).Select(AssetDatabase.GetAssetPath))
                .Distinct().ToArray();
        }

        [Test]
        public void TexturesPackedInAnAtlas_AreImportedUncompressed_SoOnlyTheAtlasCompresses()
        {
            SpriteAtlas[] atlases = LoadAll<SpriteAtlas>("t:SpriteAtlas");
            Assert.That(atlases.Length, Is.EqualTo(2), "The board atlas and the UI atlas.");
            foreach (SpriteAtlas atlas in atlases)
            {
                string[] folders = atlas.GetPackables().Select(AssetDatabase.GetAssetPath).Where(AssetDatabase.IsValidFolder).ToArray();
                string[] textures = AssetDatabase.FindAssets("t:Texture2D", folders).Select(AssetDatabase.GUIDToAssetPath).ToArray();

                Assert.That(textures, Is.Not.Empty, atlas.name);
                foreach (string path in textures)
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                    Assert.That(!android.overridden || android.format == TextureImporterFormat.RGBA32, $"{path} overrides Android with {android.format}.");
                }
            }
        }

        // Design §8.3 and the performance budget: a handful of spores, scaled and carried by the backdrop as it fits the view.
        [Test]
        public void TheBackdropsSpores_AreFew_AndScaleAndMoveWithTheBackdrop()
        {
            Backdrop backdrop = LoadAll<GameObject>("Backdrop t:Prefab").Single().GetComponent<Backdrop>();
            ParticleSystem spores = backdrop.GetComponentInChildren<ParticleSystem>(true);
            ParticleSystem.MainModule main = spores.main;

            Assert.That(main.maxParticles, Is.LessThanOrEqualTo(80));
            Assert.That(main.loop, Is.True);
            Assert.That(main.prewarm, Is.True, "The screen opens with spores already drifting.");
            Assert.That(main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Hierarchy), "The backdrop scales them with the view.");
            Assert.That(main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.Local), "They move with the camera.");
            Assert.That(spores.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Box));
            Assert.That(backdrop.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(spores.GetComponent<ParticleSystemRenderer>().sharedMaterial),
                "The gradient and the spores share one material.");
        }

        // Performance budget (SetPass <= 10) and design §8.3: uGUI batches graphics that share a texture only while they don't
        // overlap others between them, so a goal's icon and count stay apart and four goals draw in two batches, not
        // eight. 170 x 190 units is a goal's share of the card on a 16:9 phone with a notch, the smallest the HUD gets
        // (band 20 % of 1770, minus the 140-unit button row, 92 % of the card for goals; 4 goals across 1080 x 0.92 x 0.74).
        [Test]
        public void AGoalSlot_KeepsItsIconAndCountApart_AndFits40Of40()
        {
            var parent = new GameObject("Goal Area", typeof(RectTransform), typeof(Canvas));
            try
            {
                ((RectTransform)parent.transform).sizeDelta = new Vector2(170, 190);
                ObjectiveSlot prefab = LoadAll<GameObject>("ObjectiveSlot t:Prefab").Single().GetComponent<ObjectiveSlot>();
                ObjectiveSlot slot = Object.Instantiate(prefab, parent.transform);
                var rect = (RectTransform)slot.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                var icon = slot.GetComponentInChildren<UnityEngine.UI.Image>(true);
                var count = slot.GetComponentInChildren<TMPro.TMP_Text>(true);
                Rect iconRect = WorldRect(icon.rectTransform), countRect = WorldRect(count.rectTransform), slotRect = WorldRect(rect);

                Assert.That(iconRect.Overlaps(countRect), Is.False, $"icon {iconRect}, count {countRect}");
                Assert.That(Inside(iconRect, slotRect), Is.True, "The icon stays in its slot.");
                Assert.That(Inside(countRect, slotRect), Is.True, "The count stays in its slot.");
                Assert.That(count.GetPreferredValues("40/40").x, Is.LessThanOrEqualTo(countRect.width), "The largest count fits.");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

        static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void Spores_UseTheUnlitSpriteMaterial()
        {
            Assert.That(SporePrefab.sharedMaterial, Is.Not.Null);
            Assert.That(SporePrefab.sharedMaterial.name, Is.EqualTo("Sprite-Unlit-Default"));
        }
    }
}
