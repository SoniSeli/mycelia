using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Mycelia.Content
{
    /// <summary>
    /// The levels players see, in play order (design §3.6). Levels are found by their stable key, never by position,
    /// so reordering or inserting levels can't move a player's stars onto another level (ADR-0006).
    /// </summary>
    [CreateAssetMenu(menuName = "Mycelia/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        static readonly Regex KeyPattern = new Regex("^level-[0-9]{2}$");

        [SerializeField]
        List<LevelDefinition> levels = new List<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> Levels => levels;

        /// <summary>Every level's key in play order, as <c>Progression</c> takes them.</summary>
        public IReadOnlyList<string> Keys
        {
            get
            {
                var keys = new List<string>(levels.Count);
                foreach (LevelDefinition level in levels)
                {
                    if (level != null)
                        keys.Add(level.Key);
                }
                return keys;
            }
        }

        public bool TryGetByKey(string key, out LevelDefinition level)
        {
            foreach (LevelDefinition candidate in levels)
            {
                if (candidate != null && candidate.Key == key)
                {
                    level = candidate;
                    return true;
                }
            }

            level = null;
            return false;
        }

        /// <summary>Every rule the catalog or its levels break, so the Inspector and tests can show them all. Empty when valid.</summary>
        public IReadOnlyList<string> FindProblems(SporeCatalog spores)
        {
            var problems = new List<string>();

            if (spores == null)
                problems.Add("No spore catalog to check the levels against.");
            if (levels.Count == 0)
                problems.Add("The catalog has no levels.");

            var seenLevels = new HashSet<LevelDefinition>();
            var seenKeys = new HashSet<string>();

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null)
                {
                    problems.Add($"Entry {i + 1} is empty.");
                    continue;
                }

                if (!seenLevels.Add(level))
                {
                    problems.Add($"'{level.name}' is listed more than once.");
                    continue;
                }

                if (string.IsNullOrEmpty(level.Key) || !KeyPattern.IsMatch(level.Key))
                    problems.Add($"'{level.name}' needs a key of the form level-NN, e.g. level-07 (got '{level.Key}').");
                else if (!seenKeys.Add(level.Key))
                    problems.Add($"The key '{level.Key}' is used by more than one level.");

                if (spores == null)
                    continue;
                foreach (string problem in LevelDefinitionMapper.FindProblems(level, spores))
                {
                    problems.Add($"'{level.name}': {problem}");
                }
            }

            return problems;
        }

        internal void SetLevels(IEnumerable<LevelDefinition> newLevels) => levels = new List<LevelDefinition>(newLevels);
    }
}
