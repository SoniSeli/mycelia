using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mycelia.Content
{
    /// <summary>
    /// A hand-authored level (design §3.6). Mapped to the domain's <c>LevelConfig</c> by <see cref="LevelDefinitionMapper"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Mycelia/Level Definition", fileName = "level-00")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Serializable]
        public struct ObjectiveEntry
        {
            [Tooltip("Collect spores of this type.")]
            public SporeType sporeType;

            [Min(1)]
            public int target;
        }

        [SerializeField, Tooltip("Stable id used by saves and analytics, e.g. level-07. Never change it once shipped.")]
        string key;

        [Header("Board")]
        [SerializeField, Min(1)] int width = 6;
        [SerializeField, Min(1)] int height = 6;
        [SerializeField, Min(1)] int moves = 20;
        [SerializeField] List<SporeType> sporeTypes = new List<SporeType>();

        [Header("Goals")]
        [SerializeField] List<ObjectiveEntry> objectives = new List<ObjectiveEntry>();

        [SerializeField, Tooltip("Use this level's own star thresholds instead of the global 25 % / 50 %.")]
        bool overrideStarThresholds;
        [SerializeField, Range(1, 100)] int twoStarPercent = 25;
        [SerializeField, Range(1, 100)] int threeStarPercent = 50;

        [Header("Fact card")]
        [SerializeField, TextArea(3, 8)] string factText;
        [SerializeField] Sprite factImage;
        [SerializeField, Tooltip("Who made the image and under which licence.")] string factImageCredit;

        public string Key => key;
        public int Width => width;
        public int Height => height;
        public int Moves => moves;
        public IReadOnlyList<SporeType> SporeTypes => sporeTypes;
        public IReadOnlyList<ObjectiveEntry> Objectives => objectives;
        public bool OverrideStarThresholds => overrideStarThresholds;
        public int TwoStarPercent => twoStarPercent;
        public int ThreeStarPercent => threeStarPercent;
        public string FactText => factText;
        public Sprite FactImage => factImage;
        public string FactImageCredit => factImageCredit;

        internal void Set(
            string key,
            int width,
            int height,
            int moves,
            IEnumerable<SporeType> sporeTypes,
            IEnumerable<(SporeType Type, int Target)> objectives)
        {
            this.key = key;
            this.width = width;
            this.height = height;
            this.moves = moves;
            this.sporeTypes = new List<SporeType>(sporeTypes);
            this.objectives = new List<ObjectiveEntry>();
            foreach (var (type, target) in objectives)
            {
                this.objectives.Add(new ObjectiveEntry { sporeType = type, target = target });
            }
        }

        internal void SetStarThresholds(bool @override, int twoStarPercent, int threeStarPercent)
        {
            overrideStarThresholds = @override;
            this.twoStarPercent = twoStarPercent;
            this.threeStarPercent = threeStarPercent;
        }
    }
}
