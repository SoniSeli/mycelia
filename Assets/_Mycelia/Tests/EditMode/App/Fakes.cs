using System.Collections.Generic;
using System.Linq;
using Mycelia.Content;
using UnityEditor;

namespace Mycelia.App.Tests
{
    sealed class FakeSceneLoader : ISceneLoader
    {
        public readonly List<(SceneId Scene, SceneArgs Args)> Loads = new List<(SceneId, SceneArgs)>();

        public bool IsBlockingInput { get; set; }

        public void Load(SceneId scene, AppServices services, SceneArgs args = null) => Loads.Add((scene, args));
    }

    // Fades finish only when a test says so.
    sealed class FakeScreenFade : Game.IScreenFade
    {
        readonly List<string> log;
        System.Action covered;
        System.Action shown;

        public FakeScreenFade(List<string> log) => this.log = log;

        public void FadeOut(System.Action onCovered)
        {
            log.Add("fade out");
            covered = onCovered;
        }

        public void FadeIn(System.Action onShown)
        {
            log.Add("fade in");
            shown = onShown;
        }

        public void FinishFadeOut()
        {
            System.Action then = covered;
            covered = null;
            then();
        }

        public void FinishFadeIn()
        {
            System.Action then = shown;
            shown = null;
            then();
        }
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

    /// <summary>Keeps every haptic played, with room reserved so playing one allocates nothing.</summary>
    sealed class FakeHaptics : Game.IHaptics
    {
        public readonly List<Game.Haptic> Played = new List<Game.Haptic>(256);

        public void Play(Game.Haptic haptic) => Played.Add(haptic);
    }

    sealed class FakeSounds : Game.ISoundEffects
    {
        public readonly List<Game.SoundEffect> Played = new List<Game.SoundEffect>(256);
        public readonly List<int> Ticks = new List<int>(256);
        public readonly List<bool> Paused = new List<bool>();

        public void Play(Game.SoundEffect effect) => Played.Add(effect);

        public void PlayTick(int chainLength)
        {
            Played.Add(Game.SoundEffect.Tick);
            Ticks.Add(chainLength);
        }

        public void SetPaused(bool paused) => Paused.Add(paused);
    }

    // Room for every call a test makes, so recording allocates nothing.
    sealed class FakeAudioOutput : IAudioOutput
    {
        public readonly List<(Game.SoundEffect Effect, int ChainLength, float Volume)> Effects =
            new List<(Game.SoundEffect, int, float)>(256);
        public readonly List<string> Calls = new List<string>(256);

        public void PlayEffect(Game.SoundEffect effect, int chainLength, float volume)
        {
            Effects.Add((effect, chainLength, volume));
            Calls.Add("effect");
        }

        public void StopLevelEffects() => Calls.Add("stop effects");

        public void PlayMusic(float volume) => Calls.Add("music " + volume.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public void PauseMusic() => Calls.Add("pause music");
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
