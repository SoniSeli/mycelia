using System.Collections.Generic;
using System.Linq;
using Mycelia.Content;
using UnityEditor;

namespace Mycelia.App.Tests
{
    sealed class FakeSceneLoader : ISceneLoader
    {
        public readonly List<(SceneId Scene, SceneArgs Args)> Loads = new List<(SceneId, SceneArgs)>();

        public void Load(SceneId scene, AppServices services, SceneArgs args = null) => Loads.Add((scene, args));
    }

    sealed class FixedSeedSource : ISeedSource
    {
        readonly ulong seed;

        public FixedSeedSource(ulong seed) => this.seed = seed;

        public int Draws { get; private set; }

        public ulong Next()
        {
            Draws++;
            return seed;
        }
    }

    /// <summary>Keeps one saved progress in memory; can be told to fail.</summary>
    sealed class FakeProgressStore : IProgressStore
    {
        public SavedProgress Stored;
        public bool Fails;
        public int Saves;

        public ProgressLoad Load() =>
            new ProgressLoad(Stored ?? SavedProgress.Empty, Stored == null ? LoadOutcome.NoSave : LoadOutcome.Loaded, 0);

        public bool Save(SavedProgress progress)
        {
            Saves++;
            if (Fails) return false;
            Stored = progress;
            return true;
        }
    }

    /// <summary>Keeps every logged event in memory.</summary>
    sealed class FakeAnalytics : IAnalytics
    {
        public readonly List<AnalyticsEvent> Events = new List<AnalyticsEvent>();

        public void Log(AnalyticsEvent analyticsEvent) => Events.Add(analyticsEvent);

        public IEnumerable<string> Names => Events.Select(e => e.Name);

        public AnalyticsEvent Single(string name) => Events.Single(e => e.Name == name);
    }

    static class ProjectAssets
    {
        public static SporeCatalog Spores => Single<SporeCatalog>("t:SporeCatalog");

        public static LevelCatalog Levels => Single<LevelCatalog>("t:LevelCatalog");

        public static LevelDefinition TestLevel => Single<LevelDefinition>("level-test t:LevelDefinition");

        static T Single<T>(string filter) where T : UnityEngine.Object =>
            AssetDatabase.FindAssets(filter)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single();
    }
}
