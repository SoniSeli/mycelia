using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mycelia.Content.Tests
{
    /// <summary>Builds in-memory content for tests and destroys it afterwards.</summary>
    public sealed class ContentFactory : IDisposable
    {
        readonly List<Object> created = new List<Object>();

        public Sprite Sprite()
        {
            var texture = new Texture2D(4, 4);
            var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            created.Add(texture);
            created.Add(sprite);
            return sprite;
        }

        public SporeType Spore(string key, Sprite sprite = null)
        {
            var type = ScriptableObject.CreateInstance<SporeType>();
            type.name = key;
            type.Set(key, key, Color.white, sprite ?? Sprite(), key);
            created.Add(type);
            return type;
        }

        public SporeCatalog Catalog(params SporeType[] types)
        {
            var catalog = ScriptableObject.CreateInstance<SporeCatalog>();
            catalog.SetTypes(types);
            created.Add(catalog);
            return catalog;
        }

        public LevelDefinition Level(
            string key,
            SporeType[] types,
            (SporeType Type, int Target)[] objectives,
            int width = 6,
            int height = 6,
            int moves = 20)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.name = key ?? "level";
            level.Set(key, width, height, moves, types, objectives);
            created.Add(level);
            return level;
        }

        public void Dispose()
        {
            foreach (Object obj in created)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            created.Clear();
        }
    }
}
