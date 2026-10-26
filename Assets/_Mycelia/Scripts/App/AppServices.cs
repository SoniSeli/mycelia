using System;
using Mycelia.Content;
using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// The long-lived services, created once by <see cref="AppRoot"/> and passed explicitly to each scene's entry
    /// (ADR-0010). Services are interfaces, so tests can pass fakes. There is deliberately no static instance.
    /// </summary>
    public sealed class AppServices
    {
        public AppServices(ISceneLoader scenes, ISeedSource seeds, SporeCatalog spores, LevelCatalog levels, PlayerProgress progress, IAnalytics analytics,
            IHaptics haptics)
        {
            Analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            Haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
            Seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
            if (spores == null) throw new ArgumentNullException(nameof(spores));
            Spores = spores;
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            Levels = levels;
        }

        public ISceneLoader Scenes { get; }

        /// <summary>A fresh seed for each level attempt (ADR-0007).</summary>
        public ISeedSource Seeds { get; }

        /// <summary>Every spore type, to map levels and draw spores (ADR-0005).</summary>
        public SporeCatalog Spores { get; }

        /// <summary>The levels players see, in play order (design §3.6).</summary>
        public LevelCatalog Levels { get; }

        /// <summary>Unlocks and best stars, loaded at boot and saved at the ADR-0006 moments.</summary>
        public PlayerProgress Progress { get; }

        /// <summary>Playtest events, kept on the device (design §8.2 slice 6).</summary>
        public IAnalytics Analytics { get; }

        /// <summary>The device's haptics, played only while the saved haptics switch is on (ADR-0016).</summary>
        public IHaptics Haptics { get; }
    }
}
