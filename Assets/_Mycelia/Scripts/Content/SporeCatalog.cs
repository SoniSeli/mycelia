using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mycelia.Domain;
using UnityEngine;

namespace Mycelia.Content
{
    /// <summary>
    /// Every spore type in the game. Assigns each one a compact runtime <see cref="SporeTypeId"/>:
    /// position in the list + 1, since 0 means "no spore" (ADR-0005). Ids are never saved.
    /// </summary>
    [CreateAssetMenu(menuName = "Mycelia/Spore Catalog", fileName = "SporeCatalog")]
    public sealed class SporeCatalog : ScriptableObject
    {
        public const int MaxTypes = 255;

        static readonly Regex KeyPattern = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");

        [SerializeField]
        List<SporeType> types = new List<SporeType>();

        Dictionary<SporeType, SporeTypeId> idsByType;
        Dictionary<string, SporeTypeId> idsByKey;

        public IReadOnlyList<SporeType> Types => types;

        public bool TryGetId(SporeType type, out SporeTypeId id)
        {
            EnsureLookups();
            if (type != null)
                return idsByType.TryGetValue(type, out id);

            id = SporeTypeId.None;
            return false;
        }

        public SporeTypeId IdOf(SporeType type)
        {
            if (!TryGetId(type, out SporeTypeId id))
                throw new ArgumentException($"'{(type != null ? type.name : "null")}' is not in the spore catalog.", nameof(type));
            return id;
        }

        public SporeType TypeOf(SporeTypeId id)
        {
            int index = id.Value - 1;
            if (index < 0 || index >= types.Count)
                throw new ArgumentOutOfRangeException(nameof(id), id, $"The catalog has {types.Count} spore types.");
            return types[index];
        }

        public bool TryGetByKey(string key, out SporeTypeId id)
        {
            EnsureLookups();
            if (key != null)
                return idsByKey.TryGetValue(key, out id);

            id = SporeTypeId.None;
            return false;
        }

        /// <summary>Every rule the catalog breaks, so the Inspector and tests can show them all. Empty when valid.</summary>
        public IReadOnlyList<string> FindProblems()
        {
            var problems = new List<string>();

            if (types.Count > MaxTypes)
                problems.Add($"The catalog has {types.Count} spore types; the most a runtime id can address is {MaxTypes}.");

            var seenTypes = new HashSet<SporeType>();
            var seenKeys = new HashSet<string>();
            var seenSprites = new Dictionary<Sprite, SporeType>();

            for (int i = 0; i < types.Count; i++)
            {
                SporeType type = types[i];
                if (type == null)
                {
                    problems.Add($"Entry {i + 1} is empty.");
                    continue;
                }

                if (!seenTypes.Add(type))
                {
                    problems.Add($"'{type.name}' is listed more than once.");
                    continue;
                }

                if (string.IsNullOrEmpty(type.Key) || !KeyPattern.IsMatch(type.Key))
                    problems.Add($"'{type.name}' needs a key of lowercase letters, digits and hyphens (got '{type.Key}').");
                else if (!seenKeys.Add(type.Key))
                    problems.Add($"The key '{type.Key}' is used by more than one spore type.");

                if (type.Sprite == null)
                    problems.Add($"'{type.name}' has no sprite.");
                else if (seenSprites.TryGetValue(type.Sprite, out SporeType other))
                    problems.Add($"'{other.name}' and '{type.name}' share a sprite; every spore type needs its own silhouette (ADR-0005).");
                else
                    seenSprites.Add(type.Sprite, type);
            }

            return problems;
        }

        internal void SetTypes(IEnumerable<SporeType> newTypes)
        {
            types = new List<SporeType>(newTypes);
            idsByType = null;
            idsByKey = null;
        }

        // Edits in the Inspector can reorder or replace types, so the lookups are rebuilt on next use.
        void OnValidate()
        {
            idsByType = null;
            idsByKey = null;
        }

        void EnsureLookups()
        {
            if (idsByType != null)
                return;

            idsByType = new Dictionary<SporeType, SporeTypeId>(types.Count);
            idsByKey = new Dictionary<string, SporeTypeId>(types.Count);
            for (int i = 0; i < types.Count && i < MaxTypes; i++)
            {
                SporeType type = types[i];
                if (type == null || idsByType.ContainsKey(type))
                    continue;

                var id = new SporeTypeId((byte)(i + 1));
                idsByType.Add(type, id);
                if (!string.IsNullOrEmpty(type.Key) && !idsByKey.ContainsKey(type.Key))
                    idsByKey.Add(type.Key, id);
            }
        }
    }
}
