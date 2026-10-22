using System;
using System.Collections.Generic;

namespace Mycelia.Domain.Levels
{
    /// <summary>A completed level and its best result, keyed by the level's stable key (ADR-0006).</summary>
    public readonly struct LevelRecord
    {
        public readonly string Key;
        public readonly int BestStars;

        public LevelRecord(string key, int bestStars)
        {
            Key = key;
            BestStars = bestStars;
        }
    }

    /// <summary>
    /// Which levels are unlocked and their best stars (design §3.6, ADR-0006). Unlocks are derived from records:
    /// the first level is always open, and level k + 1 opens once level k has a record. Records for levels the
    /// game no longer has are kept, so saving never loses them.
    /// </summary>
    public sealed class Progression
    {
        readonly IReadOnlyList<string> levelOrder;
        readonly Dictionary<string, int> bestStars = new Dictionary<string, int>();
        readonly List<string> recordOrder = new List<string>();

        public Progression(IReadOnlyList<string> levelOrder, IEnumerable<LevelRecord> records)
        {
            this.levelOrder = levelOrder;
            foreach (LevelRecord record in records)
            {
                Store(record.Key, record.BestStars);
            }
        }

        /// <summary>Every record, including ones for unknown levels, in the order they were first stored.</summary>
        public IReadOnlyList<LevelRecord> Records
        {
            get
            {
                var records = new List<LevelRecord>(recordOrder.Count);
                foreach (string key in recordOrder)
                {
                    records.Add(new LevelRecord(key, bestStars[key]));
                }
                return records;
            }
        }

        public bool IsUnlocked(string levelKey)
        {
            int index = IndexOf(levelKey);
            return index == 0 || (index > 0 && bestStars.ContainsKey(levelOrder[index - 1]));
        }

        /// <summary>0 for a level never won.</summary>
        public int BestStars(string levelKey) => bestStars.TryGetValue(levelKey, out int stars) ? stars : 0;

        /// <summary>Records a win. Returns true if anything changed (and so needs saving).</summary>
        public bool RecordWin(string levelKey, int stars)
        {
            if (stars < 1 || stars > 3)
                throw new ArgumentOutOfRangeException(nameof(stars), stars, "A win earns 1 to 3 stars.");
            if (IndexOf(levelKey) < 0)
                throw new ArgumentException($"'{levelKey}' is not a level in this game.", nameof(levelKey));
            if (!IsUnlocked(levelKey))
                throw new InvalidOperationException($"'{levelKey}' is locked and cannot have been won.");

            if (stars <= BestStars(levelKey))
                return false;

            Store(levelKey, stars);
            return true;
        }

        void Store(string key, int stars)
        {
            if (!bestStars.ContainsKey(key))
                recordOrder.Add(key);
            bestStars[key] = stars;
        }

        int IndexOf(string levelKey)
        {
            for (int i = 0; i < levelOrder.Count; i++)
            {
                if (levelOrder[i] == levelKey)
                    return i;
            }
            return -1;
        }
    }
}
