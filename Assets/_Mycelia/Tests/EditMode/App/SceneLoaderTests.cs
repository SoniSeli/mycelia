using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class SceneLoaderTests
    {
        SceneLoader loader;
        AppServices services;
        List<string> fades;

        [SetUp]
        public void CreateLoader()
        {
            fades = new List<string>();
            loader = new SceneLoader(new FakeScreenFade(fades));
            services = new AppServices(loader, new FixedSeedSource(1), ProjectAssets.Spores, ProjectAssets.Levels, PlayerProgress.Load(new FakeProgressStore(), ProjectAssets.Levels.Keys), new FakeAnalytics());
        }

        [TearDown]
        public void DisposeLoader() => loader.Dispose();

        [Test]
        public void Bootstrap_IsNeverLoadedAgain()
        {
            Assert.Throws<ArgumentException>(() => loader.Load(SceneId.Bootstrap, services));
        }

        [Test]
        public void EveryLoad_PassesTheServices()
        {
            Assert.Throws<ArgumentNullException>(() => loader.Load(SceneId.Game, services: null));
        }

        // The fake fade never finishes, so nothing loads: the request alone blocks input and starts the fade.
        [Test]
        public void ALoadRequest_StartsTheFadeOut_AndBlocksInputAtOnce()
        {
            Assert.That(loader.IsBlockingInput, Is.False);

            loader.Load(SceneId.LevelSelect, services);

            Assert.That(fades, Is.EqualTo(new[] { "fade out" }));
            Assert.That(loader.IsBlockingInput, Is.True);
        }
    }
}
