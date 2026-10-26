using System.Linq;
using Mycelia.Domain.Levels;
using Mycelia.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Mycelia.App.Tests
{
    // Design §8.3 slice 8: the intro, on a stand-in for its scene. A first run comes from the main menu's first Play,
    // a replay from level select's help button; both end in level select, and only finishing counts as seen.
    public class IntroSceneEntryTests
    {
        GameObject root;
        FakeSceneLoader loader;
        FakeProgressStore store;
        Button next;
        BackButton back;
        GameObject[] pages;

        [SetUp]
        public void CreateRoot()
        {
            root = new GameObject("Scene");
            loader = new FakeSceneLoader();
            store = new FakeProgressStore();
        }

        [TearDown]
        public void DestroyRoot() => Object.DestroyImmediate(root);

        static T Child<T>(string name, Transform parent) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            T existing = child.GetComponent<T>();
            return existing != null ? existing : child.AddComponent<T>();
        }

        IntroSceneEntry Entry(bool introSeen)
        {
            store.Stored = new SavedProgress(new LevelRecord[0], GameSettings.Default, introSeen);

            var view = Child<IntroView>("Intro", root.transform);
            pages = Enumerable.Range(1, 3).Select(i => Child<RectTransform>($"Page {i}", view.transform).gameObject).ToArray();
            next = Child<Button>("Next", view.transform);
            var demo = Child<Transform>("Chain Demo", root.transform).gameObject.AddComponent<ChainDemo>();
            var demoSettings = new SerializedObject(demo);
            demoSettings.FindProperty("line").objectReferenceValue = Child<Transform>("Line", demo.transform).gameObject.AddComponent<LineRenderer>();
            demoSettings.FindProperty("finger").objectReferenceValue = Child<Transform>("Finger", demo.transform).gameObject.AddComponent<SpriteRenderer>();
            demoSettings.FindProperty("area").objectReferenceValue = Child<RectTransform>("Area", view.transform);
            demoSettings.ApplyModifiedPropertiesWithoutUndo();

            var viewSettings = new SerializedObject(view);
            SerializedProperty array = viewSettings.FindProperty("pages");
            array.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            viewSettings.FindProperty("next").objectReferenceValue = next;
            viewSettings.FindProperty("demo").objectReferenceValue = demo.gameObject;
            viewSettings.FindProperty("demoPage").intValue = 1;
            viewSettings.ApplyModifiedPropertiesWithoutUndo();

            back = Child<BackButton>("Back", root.transform);
            var entry = root.AddComponent<IntroSceneEntry>();
            var entrySettings = new SerializedObject(entry);
            entrySettings.FindProperty("view").objectReferenceValue = view;
            entrySettings.FindProperty("back").objectReferenceValue = back;
            entrySettings.FindProperty("demo").objectReferenceValue = demo;
            entrySettings.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        AppServices Services() =>
            new AppServices(loader, new FixedSeedSource(1), ProjectAssets.Spores, ProjectAssets.Levels,
                PlayerProgress.Load(store, ProjectAssets.Levels.Keys), new FakeAnalytics(), new FakeHaptics());

        SceneId[] Loaded => loader.Loads.Select(load => load.Scene).ToArray();

        [Test]
        public void FinishingTheIntro_OnAFirstRun_SavesItAsSeen_AndOpensLevelSelect()
        {
            IntroSceneEntry entry = Entry(introSeen: false);
            entry.Construct(Services(), new IntroSceneArgs(firstRun: true));

            next.onClick.Invoke();
            next.onClick.Invoke();
            Assert.That(loader.Loads, Is.Empty, "Two pages turned; the third is showing.");
            Assert.That(store.Saves, Is.EqualTo(0));

            next.onClick.Invoke();

            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(store.Stored.IntroSeen, Is.True);
            Assert.That(Loaded, Is.EqualTo(new[] { SceneId.LevelSelect }));
        }

        [Test]
        public void BackOnTheFirstPage_OfAFirstRun_ReturnsToTheMainMenu_AndTheIntroShowsAgainNextTime()
        {
            IntroSceneEntry entry = Entry(introSeen: false);
            AppServices services = Services();
            entry.Construct(services, new IntroSceneArgs(firstRun: true));

            back.Press();

            Assert.That(Loaded, Is.EqualTo(new[] { SceneId.MainMenu }));
            Assert.That(services.Progress.IntroSeen, Is.False);
            Assert.That(store.Saves, Is.EqualTo(0));
        }

        [Test]
        public void Back_OnALaterPage_TurnsBackAPage_WithoutLeaving()
        {
            IntroSceneEntry entry = Entry(introSeen: false);
            entry.Construct(Services(), new IntroSceneArgs(firstRun: true));
            next.onClick.Invoke();

            back.Press();

            Assert.That(loader.Loads, Is.Empty);
            Assert.That(pages.Select(page => page.activeSelf), Is.EqualTo(new[] { true, false, false }));
        }

        // The Editor's Play in the Intro scene opens it without args: it behaves as a replay.
        [TestCase(false)]
        [TestCase(null)]
        public void BackOnTheFirstPage_OfAReplay_ReturnsToLevelSelect(bool? firstRun)
        {
            IntroSceneEntry entry = Entry(introSeen: true);
            entry.Construct(Services(), firstRun.HasValue ? new IntroSceneArgs(firstRun.Value) : null);

            back.Press();

            Assert.That(Loaded, Is.EqualTo(new[] { SceneId.LevelSelect }));
        }

        [Test]
        public void FinishingAReplay_OpensLevelSelect_WithoutWritingTheSave()
        {
            IntroSceneEntry entry = Entry(introSeen: true);
            entry.Construct(Services(), new IntroSceneArgs(firstRun: false));

            for (int i = 0; i < 3; i++) next.onClick.Invoke();

            Assert.That(Loaded, Is.EqualTo(new[] { SceneId.LevelSelect }));
            Assert.That(store.Saves, Is.EqualTo(0), "Nothing changed.");
        }
    }
}
