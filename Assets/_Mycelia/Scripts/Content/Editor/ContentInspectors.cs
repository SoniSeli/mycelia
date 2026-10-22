using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Mycelia.Content.Editor
{
    /// <summary>Shows a content asset's problems above its fields, refreshed whenever a field changes.</summary>
    static class ProblemPanel
    {
        public static VisualElement Create(UnityEditor.Editor editor, Func<IReadOnlyList<string>> findProblems)
        {
            var root = new VisualElement();
            var problems = new VisualElement { style = { marginBottom = 6 } };
            root.Add(problems);
            InspectorElement.FillDefaultInspector(root, editor.serializedObject, editor);

            void Refresh()
            {
                problems.Clear();
                IReadOnlyList<string> found = findProblems();
                if (found.Count == 0)
                {
                    problems.Add(new HelpBox("No problems found.", HelpBoxMessageType.Info));
                    return;
                }
                foreach (string problem in found)
                {
                    problems.Add(new HelpBox(problem, HelpBoxMessageType.Error));
                }
            }

            Refresh();
            root.TrackSerializedObjectValue(editor.serializedObject, _ => Refresh());
            return root;
        }

        /// <summary>The project's one spore catalog, or a problem explaining why there isn't exactly one.</summary>
        public static SporeCatalog FindCatalog(List<string> problems)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(SporeCatalog));
            if (guids.Length != 1)
            {
                problems.Add($"Expected exactly one SporeCatalog in the project, found {guids.Length}.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<SporeCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }

    [CustomEditor(typeof(SporeCatalog))]
    sealed class SporeCatalogInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() =>
            ProblemPanel.Create(this, () => ((SporeCatalog)target).FindProblems());
    }

    [CustomEditor(typeof(LevelDefinition))]
    sealed class LevelDefinitionInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() =>
            ProblemPanel.Create(this, () =>
            {
                var problems = new List<string>();
                SporeCatalog catalog = ProblemPanel.FindCatalog(problems);
                if (catalog != null)
                    problems.AddRange(LevelDefinitionMapper.FindProblems((LevelDefinition)target, catalog));
                return problems;
            });
    }
}
