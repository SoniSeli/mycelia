using System;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class SceneLoaderTests
    {
        SceneLoader loader;
        AppServices services;

        [SetUp]
        public void CreateLoader()
        {
            loader = new SceneLoader();
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
    }
}
