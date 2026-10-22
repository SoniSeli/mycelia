using System;
using System.Collections.Generic;
using Mycelia.Domain;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Sessions;

namespace Mycelia.Content
{
    /// <summary>
    /// Converts a <see cref="LevelDefinition"/> asset into the domain's <see cref="LevelConfig"/> (ADR-0003, ADR-0005),
    /// checking content problems (missing assets, catalog membership) as well as the domain's own level rules.
    /// </summary>
    public static class LevelDefinitionMapper
    {
        public static LevelConfig ToConfig(LevelDefinition level, SporeCatalog catalog)
        {
            var problems = FindProblems(level, catalog);
            if (problems.Count > 0)
                throw new InvalidLevelException(level != null ? level.Key : null, problems);

            return Build(level, catalog, new List<string>());
        }

        /// <summary>Every problem with the level, in authoring terms. Empty when the level can be played.</summary>
        public static IReadOnlyList<string> FindProblems(LevelDefinition level, SporeCatalog catalog)
        {
            var problems = new List<string>();
            if (level == null)
            {
                problems.Add("No level definition.");
                return problems;
            }
            if (catalog == null)
            {
                problems.Add("No spore catalog to resolve the level's spore types.");
                return problems;
            }

            Build(level, catalog, problems);
            return problems;
        }

        // Resolves everything it can, adding problems as it goes; returns null if any were found.
        static LevelConfig Build(LevelDefinition level, SporeCatalog catalog, List<string> problems)
        {
            int before = problems.Count;

            var types = new List<SporeTypeId>(level.SporeTypes.Count);
            for (int i = 0; i < level.SporeTypes.Count; i++)
            {
                if (TryResolve(level.SporeTypes[i], catalog, $"Spore type {i + 1}", problems, out SporeTypeId id))
                    types.Add(id);
            }

            var objectives = new List<ObjectiveDefinition>(level.Objectives.Count);
            for (int i = 0; i < level.Objectives.Count; i++)
            {
                LevelDefinition.ObjectiveEntry entry = level.Objectives[i];
                if (TryResolve(entry.sporeType, catalog, $"Objective {i + 1}", problems, out SporeTypeId id))
                    objectives.Add(new ObjectiveDefinition(id, entry.target));
            }

            StarThresholds? thresholds = null;
            if (level.OverrideStarThresholds)
            {
                try
                {
                    thresholds = new StarThresholds(level.TwoStarPercent, level.ThreeStarPercent);
                }
                catch (ArgumentOutOfRangeException)
                {
                    problems.Add($"The star thresholds need 0 < two-star ({level.TwoStarPercent} %) <= three-star ({level.ThreeStarPercent} %) <= 100.");
                }
            }

            foreach (string problem in LevelConfig.FindProblems(level.Key, level.Width, level.Height, level.Moves, types, objectives))
            {
                problems.Add(WithSporeKeys(problem, catalog));
            }

            return problems.Count == before
                ? new LevelConfig(level.Key, level.Width, level.Height, level.Moves, types, objectives, thresholds)
                : null;
        }

        static bool TryResolve(SporeType type, SporeCatalog catalog, string label, List<string> problems, out SporeTypeId id)
        {
            if (type == null)
            {
                problems.Add($"{label} is empty.");
                id = SporeTypeId.None;
                return false;
            }

            if (!catalog.TryGetId(type, out id))
            {
                problems.Add($"{label} ('{type.Key}') is not in the spore catalog.");
                return false;
            }

            return true;
        }

        // The domain names spore types by runtime id ("Spore#3"); authors know them by key.
        static string WithSporeKeys(string problem, SporeCatalog catalog)
        {
            // Highest ids first, so "Spore#1" never matches inside "Spore#12".
            for (int i = catalog.Types.Count - 1; i >= 0; i--)
            {
                SporeType type = catalog.Types[i];
                if (type == null) continue;
                string id = new SporeTypeId((byte)(i + 1)).ToString();
                problem = problem.Replace(id, $"'{type.Key}'");
            }
            return problem;
        }
    }
}
