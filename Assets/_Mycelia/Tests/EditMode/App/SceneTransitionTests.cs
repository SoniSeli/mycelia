using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class SceneTransitionTests
    {
        sealed class RecordingEntry : ISceneEntry
        {
            readonly List<string> log;
            readonly Action onConstruct;

            public RecordingEntry(List<string> log, Action onConstruct = null)
            {
                this.log = log;
                this.onConstruct = onConstruct;
            }

            public AppServices Services { get; private set; }
            public SceneArgs Args { get; private set; }

            public void Construct(AppServices services, SceneArgs args)
            {
                log.Add("construct");
                Services = services;
                Args = args;
                onConstruct?.Invoke();
            }
        }

        List<string> log;
        FakeScreenFade fade;
        List<SceneId> loads;
        bool sceneIsInTheBuild;
        SceneTransition transition;
        AppServices services;

        [SetUp]
        public void CreateTransition()
        {
            log = new List<string>();
            fade = new FakeScreenFade(log);
            loads = new List<SceneId>();
            sceneIsInTheBuild = true;
            transition = new SceneTransition(fade, scene =>
            {
                log.Add("load " + scene);
                loads.Add(scene);
                return sceneIsInTheBuild;
            });
            services = new AppServices(new FakeSceneLoader(), new FixedSeedSource(1), ProjectAssets.Spores, ProjectAssets.Levels,
                PlayerProgress.Load(new FakeProgressStore(), ProjectAssets.Levels.Keys), new FakeAnalytics(), new FakeHaptics(), new FakeSounds());
        }

        [Test]
        public void ASceneChange_FadesOutFirst_AndStartsLoadingOnlyOnceTheScreenIsCovered()
        {
            transition.Request(SceneId.LevelSelect, services, null);
            Assert.That(log, Is.EqualTo(new[] { "fade out" }));

            fade.FinishFadeOut();
            Assert.That(loads, Is.EqualTo(new[] { SceneId.LevelSelect }));
        }

        [Test]
        public void WhenTheSceneHasLoaded_ItsEntryIsBuilt_ThenTheScreenFadesIn()
        {
            var args = new GameSceneArgs(ProjectAssets.Levels.Levels[0]);
            transition.Request(SceneId.Game, services, args);
            fade.FinishFadeOut();
            var entry = new RecordingEntry(log);

            transition.Arrived("Game", () => entry);

            Assert.That(log, Is.EqualTo(new[] { "fade out", "load Game", "construct", "fade in" }));
            Assert.That(entry.Services, Is.SameAs(services));
            Assert.That(entry.Args, Is.SameAs(args));
        }

        [Test]
        public void TheScreenIsChanging_FromTheRequest_UntilTheNextSceneHasFadedIn()
        {
            Assert.That(transition.IsChangingScene, Is.False);

            transition.Request(SceneId.MainMenu, services, null);
            Assert.That(transition.IsChangingScene, Is.True, "fading out");
            fade.FinishFadeOut();
            Assert.That(transition.IsChangingScene, Is.True, "loading");
            transition.Arrived("MainMenu", () => new RecordingEntry(log));
            Assert.That(transition.IsChangingScene, Is.True, "fading in");

            fade.FinishFadeIn();
            Assert.That(transition.IsChangingScene, Is.False);
        }

        // Nothing can be pressed mid-fade: a second tap, or back, asks for another scene while the first is on its way.
        [Test]
        public void ARequestWhileTheScreenIsChanging_IsIgnored_TheFirstOneWins()
        {
            transition.Request(SceneId.LevelSelect, services, null);
            transition.Request(SceneId.Game, services, null);
            fade.FinishFadeOut();
            transition.Request(SceneId.Game, services, null);
            transition.Arrived("LevelSelect", () => new RecordingEntry(log));
            transition.Request(SceneId.MainMenu, services, null);

            Assert.That(loads, Is.EqualTo(new[] { SceneId.LevelSelect }));
            Assert.That(log, Is.EqualTo(new[] { "fade out", "load LevelSelect", "construct", "fade in" }));
        }

        [Test]
        public void AfterTheScreenHasFadedIn_TheNextChangeIsTaken()
        {
            transition.Request(SceneId.LevelSelect, services, null);
            fade.FinishFadeOut();
            transition.Arrived("LevelSelect", () => new RecordingEntry(log));
            fade.FinishFadeIn();

            transition.Request(SceneId.Game, services, null);
            fade.FinishFadeOut();

            Assert.That(loads, Is.EqualTo(new[] { SceneId.LevelSelect, SceneId.Game }));
        }

        [Test]
        public void AnEntryMayOpenAnotherScene_FromConstruct_AndTheScreenStaysCovered()
        {
            transition.Request(SceneId.MainMenu, services, null);
            fade.FinishFadeOut();

            transition.Arrived("MainMenu", () => new RecordingEntry(log, () => transition.Request(SceneId.LevelSelect, services, null)));
            fade.FinishFadeOut();

            Assert.That(log, Is.EqualTo(new[] { "fade out", "load MainMenu", "construct", "fade out", "load LevelSelect" }));
            Assert.That(transition.IsChangingScene, Is.True);
        }

        [Test]
        public void AnotherScenesLoading_IsNotMistakenForTheRequestedOne()
        {
            transition.Request(SceneId.MainMenu, services, null);
            fade.FinishFadeOut();

            transition.Arrived("Game", () => new RecordingEntry(log));

            Assert.That(log, Has.No.Member("construct"));
        }

        [Test]
        public void ASceneMissingFromTheBuild_FailsLoudly_AndLeavesNoChangePending()
        {
            sceneIsInTheBuild = false;
            transition.Request(SceneId.Game, services, null);

            Assert.Throws<InvalidOperationException>(() => fade.FinishFadeOut());
            Assert.That(transition.IsChangingScene, Is.False);
        }

        [Test]
        public void Bootstrap_IsNeverOpenedAgain_AndEveryChangeCarriesTheServices()
        {
            Assert.Throws<ArgumentException>(() => transition.Request(SceneId.Bootstrap, services, null));
            Assert.Throws<ArgumentNullException>(() => transition.Request(SceneId.Game, null, null));
            Assert.That(log, Is.Empty);
        }
    }
}
