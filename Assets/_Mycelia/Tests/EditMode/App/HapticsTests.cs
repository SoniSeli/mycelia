using System;
using System.Linq;
using System.Xml;
using Mycelia.App.Editor;
using Mycelia.Game;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.App.Tests
{
    public class HapticsTests
    {
        PlayerProgress progress;
        FakeHaptics device;
        SwitchedHaptics haptics;

        [SetUp]
        public void Create()
        {
            progress = PlayerProgress.Load(new FakeProgressStore(), ProjectAssets.Levels.Keys);
            device = new FakeHaptics();
            haptics = new SwitchedHaptics(progress, device);
        }

        void SetSwitch(bool on) =>
            progress.ChangeSettings(new GameSettings(progress.Settings.SfxVolume, progress.Settings.MusicVolume, on));

        [Test]
        public void WhileTheSwitchIsOn_HapticsReachTheDevice()
        {
            SetSwitch(true);

            haptics.Play(Haptic.Tick);
            haptics.Play(Haptic.Win);

            Assert.That(device.Played, Is.EqualTo(new[] { Haptic.Tick, Haptic.Win }));
        }

        [Test]
        public void WithTheSwitchOff_NothingIsPlayed()
        {
            SetSwitch(false);

            foreach (Haptic haptic in Enum.GetValues(typeof(Haptic)))
                haptics.Play(haptic);

            Assert.That(device.Played, Is.Empty);
        }

        [Test]
        public void TheSwitch_AppliesAtOnce_WithoutRebuildingAnything()
        {
            SetSwitch(true);
            haptics.Play(Haptic.Tick);
            SetSwitch(false);
            haptics.Play(Haptic.Tick);
            SetSwitch(true);
            haptics.Play(Haptic.Commit);

            Assert.That(device.Played, Is.EqualTo(new[] { Haptic.Tick, Haptic.Commit }));
        }

        [Test]
        public void PlayingAHaptic_AllocatesNoMemory()
        {
            SetSwitch(true);
            haptics.Play(Haptic.Tick);

            Assert.That(() =>
            {
                haptics.Play(Haptic.Tick);
                haptics.Play(Haptic.Commit);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void EveryHaptic_HasAStrength_AndTheyGrowFromTickToWin()
        {
            AndroidHaptics.Strength tick = AndroidHaptics.For(Haptic.Tick);
            AndroidHaptics.Strength commit = AndroidHaptics.For(Haptic.Commit);
            AndroidHaptics.Strength win = AndroidHaptics.For(Haptic.Win);

            foreach (Haptic haptic in Enum.GetValues(typeof(Haptic)))
            {
                AndroidHaptics.Strength strength = AndroidHaptics.For(haptic);
                Assert.That(strength.Milliseconds, Is.InRange(1, 200), haptic.ToString());
                Assert.That(strength.Amplitude, Is.InRange(1, 255), haptic.ToString());
            }
            Assert.That(tick.Amplitude, Is.LessThan(commit.Amplitude));
            Assert.That(commit.Amplitude, Is.LessThan(win.Amplitude));
            Assert.That(new[] { tick.Predefined, commit.Predefined, win.Predefined },
                Is.EqualTo(new[] { AndroidHaptics.EffectTick, AndroidHaptics.EffectClick, AndroidHaptics.Strength.OneShotOnly }));
            Assert.That(win.Milliseconds, Is.GreaterThan(commit.Milliseconds));
        }

        const string Manifest =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"com.unity3d.player\">\n" +
            "  <application android:extractNativeLibs=\"true\" />\n" +
            "</manifest>";

        static XmlDocument Load(string xml)
        {
            var document = new XmlDocument();
            document.LoadXml(xml);
            return document;
        }

        static int VibratePermissions(XmlDocument manifest) =>
            manifest.DocumentElement.ChildNodes.OfType<XmlElement>()
                .Count(e => e.Name == "uses-permission" && e.GetAttribute("name", "http://schemas.android.com/apk/res/android") == "android.permission.VIBRATE");

        [Test]
        public void TheAndroidBuild_DeclaresTheVibratePermission_Once()
        {
            XmlDocument manifest = Load(Manifest);

            Assert.That(AndroidVibratePermission.AddTo(manifest), Is.True);
            Assert.That(AndroidVibratePermission.AddTo(manifest), Is.False);

            Assert.That(VibratePermissions(Load(manifest.OuterXml)), Is.EqualTo(1));
        }
    }
}
