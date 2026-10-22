using System;
using System.Collections.Generic;
using Mycelia.Domain.Objectives;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Levels
{
    /// <summary>"Collect <see cref="Target"/> spores of <see cref="Type"/>", the only objective in v1 (design §3.5).</summary>
    public readonly struct ObjectiveDefinition
    {
        public readonly SporeTypeId Type;
        public readonly int Target;

        public ObjectiveDefinition(SporeTypeId type, int target)
        {
            Type = type;
            Target = target;
        }
    }

    public sealed class InvalidLevelException : Exception
    {
        public IReadOnlyList<string> Problems { get; }

        public InvalidLevelException(string key, IReadOnlyList<string> problems)
            : base($"Level '{key}' is invalid: {string.Join(" ", problems)}") => Problems = problems;
    }

    /// <summary>
    /// A level as the domain sees it: plain C#, mapped from the level's ScriptableObject at load (ADR-0003, ADR-0005).
    /// </summary>
    public sealed class LevelConfig
    {
        readonly ObjectiveDefinition[] objectives;

        public string Key { get; }
        public int Width { get; }
        public int Height { get; }
        public int Moves { get; }
        public IReadOnlyList<SporeTypeId> SporeTypes { get; }
        public IReadOnlyList<ObjectiveDefinition> Objectives => objectives;
        public StarThresholds StarThresholds { get; }

        public LevelConfig(
            string key,
            int width,
            int height,
            int moves,
            IReadOnlyList<SporeTypeId> sporeTypes,
            IReadOnlyList<ObjectiveDefinition> objectives,
            StarThresholds? starThresholds = null)
        {
            var problems = FindProblems(key, width, height, moves, sporeTypes, objectives);
            if (problems.Count > 0)
                throw new InvalidLevelException(key, problems);

            Key = key;
            Width = width;
            Height = height;
            Moves = moves;
            SporeTypes = Copy(sporeTypes);
            this.objectives = Copy(objectives);
            StarThresholds = starThresholds ?? StarThresholds.Default;
        }

        /// <summary>New objective instances with no progress, for a fresh attempt.</summary>
        public IObjective[] CreateObjectives()
        {
            var created = new IObjective[objectives.Length];
            for (int i = 0; i < objectives.Length; i++)
            {
                created[i] = new CollectSporesObjective(objectives[i].Type, objectives[i].Target);
            }
            return created;
        }

        /// <summary>Every rule a level breaks, so authoring tools can show them all at once. Empty when valid.</summary>
        public static IReadOnlyList<string> FindProblems(
            string key,
            int width,
            int height,
            int moves,
            IReadOnlyList<SporeTypeId> sporeTypes,
            IReadOnlyList<ObjectiveDefinition> objectives)
        {
            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(key))
                problems.Add("The level needs a key.");

            if (width <= 0 || height <= 0 || width * height < 2)
                problems.Add($"A {width} × {height} board has no room for a chain of 2.");

            if (moves <= 0)
                problems.Add("The level needs at least one move.");

            if (sporeTypes.Count == 0)
                problems.Add("The level needs at least one spore type.");

            var seen = new HashSet<SporeTypeId>();
            for (int i = 0; i < sporeTypes.Count; i++)
            {
                if (sporeTypes[i] == SporeTypeId.None)
                    problems.Add("The empty spore type cannot be used in a level.");
                else if (!seen.Add(sporeTypes[i]))
                    problems.Add($"{sporeTypes[i]} is listed more than once.");
            }

            if (objectives.Count == 0)
                problems.Add("The level needs at least one objective.");

            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i].Target <= 0)
                    problems.Add($"Objective {i + 1} needs a target greater than zero.");
                if (!seen.Contains(objectives[i].Type))
                    problems.Add($"Objective {i + 1} asks for {objectives[i].Type}, which is not one of the level's spore types.");
            }

            return problems;
        }

        static T[] Copy<T>(IReadOnlyList<T> source)
        {
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }
            return copy;
        }
    }
}
