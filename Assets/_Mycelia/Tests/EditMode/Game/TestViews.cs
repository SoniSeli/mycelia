using System.Linq;
using Mycelia.Content;
using UnityEditor;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    /// <summary>Scene-object templates standing in for the view prefabs, and the project's spore catalog.</summary>
    static class TestViews
    {
        public static GameObject SporeTemplate()
        {
            var template = new GameObject("Spore Template");
            var view = template.AddComponent<SporeView>();
            SetReference(view, "spriteRenderer", template.AddComponent<SpriteRenderer>());
            return template;
        }

        public static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static SporeCatalog ProjectSpores =>
            AssetDatabase.FindAssets("t:SporeCatalog")
                .Select(guid => AssetDatabase.LoadAssetAtPath<SporeCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single();
    }
}
