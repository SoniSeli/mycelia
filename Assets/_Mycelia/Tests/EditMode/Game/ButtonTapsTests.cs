using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    public class ButtonTapsTests
    {
        GameObject root;

        [SetUp]
        public void CreateRoot() => root = new GameObject("Screen");

        [TearDown]
        public void DestroyRoot() => Object.DestroyImmediate(root);

        Button Child(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Button));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Button>();
        }

        [Test]
        public void EveryButtonInTheScene_TapsOncePerPress_HiddenOnesToo()
        {
            Button play = Child("Play", root.transform);
            Button hidden = Child("Close", Child("Modal", root.transform).transform);
            hidden.transform.parent.gameObject.SetActive(false);
            var sounds = new RecordingSounds();

            ButtonTaps.Attach(root.scene, sounds);
            play.onClick.Invoke();
            hidden.onClick.Invoke();
            play.onClick.Invoke();

            Assert.That(sounds.Played, Is.EqualTo(new[] { SoundEffect.Tap, SoundEffect.Tap, SoundEffect.Tap }));
        }

        [Test]
        public void TheButtonsOwnAction_StillRuns()
        {
            Button play = Child("Play", root.transform);
            int pressed = 0;
            play.onClick.AddListener(() => pressed++);

            ButtonTaps.Attach(root.scene, new RecordingSounds());
            play.onClick.Invoke();

            Assert.That(pressed, Is.EqualTo(1));
        }
    }
}
