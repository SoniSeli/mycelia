using System.Linq;
using NUnit.Framework;
using UnityEditor;
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
