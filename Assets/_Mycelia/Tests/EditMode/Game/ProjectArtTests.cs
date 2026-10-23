using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Mycelia.Game.Tests
{
    /// <summary>Guards the board's real prefabs and atlas (ADR-0002, ADR-0011).</summary>
    public class ProjectArtTests
    {
        static T[] LoadAll<T>(string filter) where T : Object =>
            AssetDatabase.FindAssets(filter)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

        static SpriteRenderer SporePrefab =>
            LoadAll<GameObject>("t:Prefab").Select(prefab => prefab.GetComponent<SporeView>()).Single(view => view != null).Renderer;

        static SpriteRenderer CellPrefab => LoadAll<GameObject>("Cell t:Prefab").Single().GetComponent<SpriteRenderer>();

        [Test]
        public void OneAtlas_PacksEverySporeSprite_AndTheCellTile()
        {
            SpriteAtlas atlas = LoadAll<SpriteAtlas>("t:SpriteAtlas").Single();
            var sprites = TestViews.ProjectSpores.Types.Select(type => type.Sprite).Append(CellPrefab.sprite);

            foreach (Sprite sprite in sprites)
            {
                Assert.That(sprite, Is.Not.Null);
                Assert.That(atlas.CanBindTo(sprite), Is.True, $"{sprite.name} is not in {atlas.name}.");
            }
        }

        [Test]
        public void TexturesPackedInTheAtlas_AreImportedUncompressed_SoOnlyTheAtlasCompresses()
        {
            SpriteAtlas atlas = LoadAll<SpriteAtlas>("t:SpriteAtlas").Single();
            string[] folders = atlas.GetPackables().Select(AssetDatabase.GetAssetPath).Where(AssetDatabase.IsValidFolder).ToArray();
            string[] textures = AssetDatabase.FindAssets("t:Texture2D", folders).Select(AssetDatabase.GUIDToAssetPath).ToArray();

            Assert.That(textures, Is.Not.Empty);
            foreach (string path in textures)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.That(!android.overridden || android.format == TextureImporterFormat.RGBA32, $"{path} overrides Android with {android.format}.");
            }
        }

        [Test]
        public void BoardSprites_UseTheUnlitSpriteMaterial()
        {
            foreach (SpriteRenderer renderer in new[] { SporePrefab, CellPrefab })
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null, renderer.name);
                Assert.That(renderer.sharedMaterial.name, Is.EqualTo("Sprite-Unlit-Default"), renderer.name);
            }
        }
    }
}
