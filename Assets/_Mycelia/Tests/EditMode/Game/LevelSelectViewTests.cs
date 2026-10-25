using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    public class LevelSelectViewTests
    {
        readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void DestroyAll()
        {
            foreach (GameObject obj in created) Object.DestroyImmediate(obj);
            created.Clear();
        }

        LevelButton Button()
        {
            LevelButton button = TestMenus.Button();
            created.Add(button.gameObject);
            return button;
        }

        static string Number(LevelButton button) => button.transform.Find("Number").GetComponent<TMP_Text>().text;
        static GameObject Lock(LevelButton button) => button.transform.Find("Lock").gameObject;
        static Image[] Stars(LevelButton button) => Enumerable.Range(1, 3).Select(i => button.transform.Find($"Star {i}").GetComponent<Image>()).ToArray();

        [Test]
        public void AnUnlockedLevel_ShowsItsNumberAndBestStars_AndCanBeTapped()
        {
            LevelButton button = Button();
            int clicks = 0;

            button.Show(new LevelButtonState(number: 4, unlocked: true, bestStars: 2), () => clicks++);
            button.GetComponent<Button>().onClick.Invoke();

            Assert.That(Number(button), Is.EqualTo("4"));
            Assert.That(Lock(button).activeSelf, Is.False);
            Assert.That(Stars(button).Select(star => star.color), Is.EqualTo(new[] { button.EarnedStarColour, button.EarnedStarColour, button.MissingStarColour }));
            Assert.That(button.GetComponent<Button>().interactable, Is.True);
            Assert.That(clicks, Is.EqualTo(1));
        }

        [Test]
        public void AnUnlockedLevelNeverWon_ShowsThreeUnlitStars()
        {
            LevelButton button = Button();

            button.Show(new LevelButtonState(1, unlocked: true, bestStars: 0), () => { });

            Assert.That(Stars(button).All(star => star.gameObject.activeSelf && star.color == button.MissingStarColour));
        }

        [Test]
        public void ALockedLevel_ShowsALock_WithoutNumberOrStars_AndCannotBeTapped()
        {
            LevelButton button = Button();

            button.Show(new LevelButtonState(5, unlocked: false, bestStars: 0), () => { });

            Assert.That(Lock(button).activeSelf, Is.True);
            Assert.That(button.transform.Find("Number").gameObject.activeSelf, Is.False);
            Assert.That(Stars(button).All(star => !star.gameObject.activeSelf), "No stars on a locked level.");
            Assert.That(button.GetComponent<Button>().interactable, Is.False);
        }

        [Test]
        public void TheView_HasOneButtonPerLevel_InPlayOrder()
        {
            LevelSelectView view = TestMenus.LevelSelect(out _);
            created.Add(view.gameObject);

            view.Init(new[]
            {
                new LevelButtonState(1, true, 3),
                new LevelButtonState(2, true, 0),
                new LevelButtonState(3, false, 0),
            }, _ => { }, () => { });

            Assert.That(view.Buttons.Count, Is.EqualTo(3));
            Assert.That(view.Buttons.Select(Number), Is.EqualTo(new[] { "1", "2", "3" }));
            Assert.That(view.Buttons.Select(b => b.transform.parent.name), Has.All.EqualTo("Grid"));
            Assert.That(view.Buttons.All(b => b.gameObject.activeSelf), "The template is hidden; its copies are not.");
        }

        [Test]
        public void TappingALevel_ChoosesItsPositionInTheCatalog()
        {
            LevelSelectView view = TestMenus.LevelSelect(out _);
            created.Add(view.gameObject);
            var chosen = new List<int>();
            view.Init(new[] { new LevelButtonState(1, true, 1), new LevelButtonState(2, true, 0) }, chosen.Add, () => { });

            view.Buttons[1].GetComponent<Button>().onClick.Invoke();

            Assert.That(chosen, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Play_OnTheMainMenu_AsksToOpenLevelSelect()
        {
            MainMenuView menu = TestMenus.MainMenu();
            created.Add(menu.gameObject);
            int plays = 0;
            menu.Init(() => plays++, () => { });

            menu.GetComponentInChildren<Button>().onClick.Invoke();

            Assert.That(plays, Is.EqualTo(1));
        }

        // Design §8.3 slice 5: the settings modal opens from the main menu and level select too.
        [Test]
        public void TheSettingsButtons_OnBothMenus_AskToOpenSettings()
        {
            MainMenuView menu = TestMenus.MainMenu();
            LevelSelectView select = TestMenus.LevelSelect(out _);
            created.Add(menu.gameObject);
            created.Add(select.gameObject);
            int opened = 0;
            menu.Init(() => { }, () => opened++);
            select.Init(new[] { new LevelButtonState(1, true, 0) }, _ => { }, () => opened++);

            menu.transform.Find("Settings").GetComponent<Button>().onClick.Invoke();
            select.transform.Find("Settings").GetComponent<Button>().onClick.Invoke();

            Assert.That(opened, Is.EqualTo(2));
        }
    }
}
