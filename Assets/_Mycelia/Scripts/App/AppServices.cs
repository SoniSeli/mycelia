using System;
using Mycelia.Content;

namespace Mycelia.App
{
    /// <summary>
    /// The long-lived services, created once by <see cref="AppRoot"/> and passed explicitly to each scene's entry
    /// (ADR-0010). Services are interfaces, so tests can pass fakes. There is deliberately no static instance.
    /// </summary>
    public sealed class AppServices
    {
        public AppServices(ISceneLoader scenes, ISeedSource seeds, SporeCatalog spores)
        {
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
            Seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
            if (spores == null) throw new ArgumentNullException(nameof(spores));
            Spores = spores;
        }

        public ISceneLoader Scenes { get; }

        /// <summary>A fresh seed for each level attempt (ADR-0007).</summary>
        public ISeedSource Seeds { get; }

        /// <summary>Every spore type, to map levels and draw spores (ADR-0005).</summary>
        public SporeCatalog Spores { get; }
    }
}
