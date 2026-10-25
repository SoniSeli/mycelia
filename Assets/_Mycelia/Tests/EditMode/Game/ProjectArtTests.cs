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

        [Test]
        public void Spores_UseTheUnlitSpriteMaterial()
        {
            Assert.That(SporePrefab.sharedMaterial, Is.Not.Null);
            Assert.That(SporePrefab.sharedMaterial.name, Is.EqualTo("Sprite-Unlit-Default"));
        }
    }
}
