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
    /// <summary>The main menu and level select entries, on stand-ins for their scenes.</summary>
    public class MenuSceneEntryTests
    {
        GameObject root;
        FakeSceneLoader loader;
        FakeProgressStore store;

        [SetUp]
        public void CreateRoot()
        {
            root = new GameObject("Scene");
            loader = new FakeSceneLoader();
            store = new FakeProgressStore();
        }

        [TearDown]
        public void DestroyRoot() => Object.DestroyImmediate(root);

        AppServices Services() =>
            new AppServices(loader, new FixedSeedSource(1), ProjectAssets.Spores, ProjectAssets.Levels,
                PlayerProgress.Load(store, ProjectAssets.Levels.Keys), new FakeAnalytics(), new FakeHaptics());

        static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static T Child<T>(string name, Transform parent) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }

        BackButton Back()
        {
            var back = Child<BackButton>("Back", root.transform);
            return back;
        }

        LevelButton ButtonTemplate(Transform parent)
        {
            var template = Child<LevelButton>("Level Button", parent);
            Set(template, "node", template.gameObject.AddComponent<Image>());
            Set(template, "button", template.gameObject.AddComponent<Button>());
            Set(template, "number", Child<TMPro.TextMeshProUGUI>("Number", template.transform));
            Set(template, "lockIcon", Child<Image>("Lock", template.transform));
            template.gameObject.SetActive(false);
            return template;
        }

        (LevelSelectSceneEntry Entry, LevelSelectView View, BackButton Back, TestSettingsModal Settings, Button SettingsButton) LevelSelect()
        {
            var entry = root.AddComponent<LevelSelectSceneEntry>();
            var view = Child<LevelSelectView>("Level Select", root.transform);
            Set(view, "buttonPrefab", ButtonTemplate(view.transform));
            Set(view, "grid", Child<RectTransform>("Grid", view.transform));
            Set(view, "help", Child<Button>("Help Button", view.transform));
            var settingsButton = Child<Button>("Settings Button", view.transform);
            Set(view, "settings", settingsButton);
            BackButton back = Back();
            var settings = new TestSettingsModal(root.transform);
            Set(entry, "view", view);
            Set(entry, "back", back);
            Set(entry, "settings", settings.Modal);
            return (entry, view, back, settings, settingsButton);
        }

        (MainMenuSceneEntry Entry, Button Play, BackButton Back, TestSettingsModal Settings, Button SettingsButton) MainMenu()
        {
            var entry = root.AddComponent<MainMenuSceneEntry>();
            var view = Child<MainMenuView>("Main Menu", root.transform);
            var play = Child<Image>("Play", view.transform).gameObject.AddComponent<Button>();
            var settingsButton = Child<Button>("Settings Button", view.transform);
            Set(view, "play", play);
            Set(view, "settings", settingsButton);
            BackButton back = Back();
            var settings = new TestSettingsModal(root.transform);
            Set(entry, "view", view);
            Set(entry, "back", back);
            Set(entry, "settings", settings.Modal);
            return (entry, play, back, settings, settingsButton);
        }

        [Test]
        public void Play_OnTheMainMenu_OpensLevelSelect_OnceTheIntroWasSeen()
        {
            store.Stored = new SavedProgress(new LevelRecord[0], GameSettings.Default, introSeen: true);
            var (entry, play, _, _, _) = MainMenu();
            AppServices services = Services();

            entry.Construct(services, args: null);
            play.onClick.Invoke();

            Assert.That(loader.Loads.Select(load => load.Scene), Is.EqualTo(new[] { SceneId.LevelSelect }));
        }

        // Design §8.3 slice 8: the first Play shows the intro, which then leads into level select.
        [Test]
        public void Play_OnTheFirstLaunch_OpensTheIntro_AsAFirstRun()
        {
            store.Stored = new SavedProgress(new LevelRecord[0], GameSettings.Default, introSeen: false);
            var (entry, play, _, _, _) = MainMenu();
            entry.Construct(Services(), args: null);

            play.onClick.Invoke();

            Assert.That(loader.Loads.Select(load => load.Scene), Is.EqualTo(new[] { SceneId.Intro }));
            Assert.That(((IntroSceneArgs)loader.Loads[0].Args).FirstRun, Is.True);
        }

        [Test]
        public void Help_OnLevelSelect_ReplaysTheIntro()
        {
            store.Stored = new SavedProgress(new LevelRecord[0], GameSettings.Default, introSeen: true);
            var (entry, view, _, _, _) = LevelSelect();
            entry.Construct(Services(), args: null);

            view.transform.Find("Help Button").GetComponent<Button>().onClick.Invoke();

            Assert.That(loader.Loads.Select(load => load.Scene), Is.EqualTo(new[] { SceneId.Intro }));
            Assert.That(((IntroSceneArgs)loader.Loads[0].Args).FirstRun, Is.False);
        }

        // Design §8.3 slice 5: the settings modal opens on the menus with the saved settings, and back closes it first.
        [Test]
        public void Settings_OnTheMainMenu_OpenWithTheSavedChoice_AndBackClosesThemFirst()
        {
            store.Stored = new SavedProgress(new LevelRecord[0], new GameSettings(0.8f, 0f, true), introSeen: false);
            var (entry, _, back, settings, settingsButton) = MainMenu();
            entry.Construct(Services(), args: null);

            settingsButton.onClick.Invoke();
            Assert.That(settings.Modal.IsOpen, Is.True);
            Assert.That(settings.Music.isOn, Is.False, "Music was saved at volume 0.");

            back.Press();
            Assert.That(settings.Modal.IsOpen, Is.False);
            Assert.That(loader.Loads, Is.Empty, "Back closed settings and did nothing else.");
        }

        [Test]
        public void FlippingASwitch_OnLevelSelect_SavesIt_AndBackThenClosesBeforeLeaving()
        {
            var (entry, _, back, settings, settingsButton) = LevelSelect();
            entry.Construct(Services(), args: null);
            settingsButton.onClick.Invoke();

            settings.Haptics.isOn = false;
            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(store.Stored.Settings.Haptics, Is.False);

            back.Press();
            Assert.That(loader.Loads, Is.Empty);
            back.Press();
            Assert.That(loader.Loads.Select(load => load.Scene), Is.EqualTo(new[] { SceneId.MainMenu }));
        }

        [Test]
        public void LevelSelect_ShowsEveryCatalogLevel_WithTheSavedProgress()
        {
            store.Stored = new SavedProgress(new[] { new LevelRecord("level-01", 2) }, GameSettings.Default, introSeen: false);
            var (entry, view, _, _, _) = LevelSelect();

            entry.Construct(Services(), args: null);

            Assert.That(view.Buttons.Count, Is.EqualTo(ProjectAssets.Levels.Levels.Count));
            Assert.That(view.Buttons[0].IsUnlocked, Is.True);
            Assert.That(view.Buttons[1].IsUnlocked, Is.True, "Winning level-01 opened level-02.");
            Assert.That(view.Buttons[2].IsUnlocked, Is.False);
        }

        [Test]
        public void ChoosingALevel_OpensTheGameOnThatLevel()
        {
            store.Stored = new SavedProgress(new[] { new LevelRecord("level-01", 2) }, GameSettings.Default, introSeen: false);
            var (entry, view, _, _, _) = LevelSelect();
            entry.Construct(Services(), args: null);

            view.Buttons[1].GetComponent<Button>().onClick.Invoke();

            Assert.That(loader.Loads.Count, Is.EqualTo(1));
            Assert.That(loader.Loads[0].Scene, Is.EqualTo(SceneId.Game));
            Assert.That(((GameSceneArgs)loader.Loads[0].Args).Level, Is.SameAs(ProjectAssets.Levels.Levels[1]));
        }

        [Test]
        public void Back_OnLevelSelect_ReturnsToTheMainMenu()
        {
            var (entry, _, back, _, _) = LevelSelect();
            entry.Construct(Services(), args: null);

            back.Press();

            Assert.That(loader.Loads.Select(load => load.Scene), Is.EqualTo(new[] { SceneId.MainMenu }));
        }
    }
}
