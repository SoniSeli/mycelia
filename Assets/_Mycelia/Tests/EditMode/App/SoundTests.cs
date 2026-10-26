using System;
using Mycelia.Game;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.App.Tests
{
    public class SoundTests
    {
        PlayerProgress progress;
        FakeAudioOutput output;
        SwitchedSoundEffects sounds;
        Music music;

        [SetUp]
        public void Create()
        {
            progress = PlayerProgress.Load(new FakeProgressStore(), ProjectAssets.Levels.Keys);
            output = new FakeAudioOutput();
            sounds = new SwitchedSoundEffects(progress, output);
            music = new Music(progress, output);
        }

        void SetVolumes(float effects, float music) =>
            progress.ChangeSettings(new GameSettings(effects, music, progress.Settings.Haptics));

        [Test]
        public void WhileTheEffectsSwitchIsOn_EffectsPlayAtTheSavedVolume()
        {
            SetVolumes(effects: 0.8f, music: 0);

            sounds.PlayTick(3);
            sounds.Play(SoundEffect.Win);

            Assert.That(output.Effects, Is.EqualTo(new[] { (SoundEffect.Tick, 3, 0.8f), (SoundEffect.Win, 0, 0.8f) }));
        }

        [Test]
        public void WithTheEffectsSwitchOff_NothingIsPlayed()
        {
            SetVolumes(effects: 0, music: 0.6f);

            sounds.PlayTick(1);
            foreach (SoundEffect effect in Enum.GetValues(typeof(SoundEffect)))
                sounds.Play(effect);

            Assert.That(output.Effects, Is.Empty);
        }

        [Test]
        public void TheEffectsSwitch_AppliesAtOnce_WithoutRebuildingAnything()
        {
            SetVolumes(effects: 0.8f, music: 0);
            sounds.Play(SoundEffect.Tap);
            SetVolumes(effects: 0, music: 0);
            sounds.Play(SoundEffect.Tap);
            SetVolumes(effects: 0.5f, music: 0);
            sounds.Play(SoundEffect.Commit);

            Assert.That(output.Effects, Is.EqualTo(new[] { (SoundEffect.Tap, 0, 0.8f), (SoundEffect.Commit, 0, 0.5f) }));
        }

        // ADR-0013: nothing of the level keeps sounding under the pause overlay.
        [Test]
        public void PausingTheLevel_StopsItsEffects_AndResumingStartsNothing()
        {
            sounds.SetPaused(true);
            sounds.SetPaused(false);

            Assert.That(output.Calls, Is.EqualTo(new[] { "stop effects" }));
        }

        [Test]
        public void PlayingAnEffect_AllocatesNoMemory()
        {
            SetVolumes(effects: 0.8f, music: 0);
            sounds.PlayTick(1);

            Assert.That(() =>
            {
                sounds.PlayTick(2);
                sounds.Play(SoundEffect.Commit);
            }, Is.Not.AllocatingGCMemory());
        }

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(12, 11)]
        [TestCase(13, 11)]
        [TestCase(49, 11)]
        public void EachSporeInTheChain_TicksAStepHigher_UntilTheTopStep(int chainLength, int tick)
        {
            Assert.That(AudioPlayer.TickIndex(chainLength, tickCount: 12), Is.EqualTo(tick));
        }

        [Test]
        public void TheMusic_PlaysAtTheSavedVolume_FromTheStart()
        {
            SetVolumes(effects: 0.8f, music: 0.6f);

            music.Follow();

            Assert.That(output.Calls, Is.EqualTo(new[] { "music 0.6" }));
        }

        [Test]
        public void SwitchingTheMusicOff_PausesIt_AndOnAgainCarriesOn()
        {
            SetVolumes(effects: 0.8f, music: 0.6f);
            music.Follow();

            SetVolumes(effects: 0.8f, music: 0);
            music.Follow();
            SetVolumes(effects: 0.8f, music: 0.6f);
            music.Follow();

            Assert.That(output.Calls, Is.EqualTo(new[] { "music 0.6", "pause music", "music 0.6" }));
        }

        [Test]
        public void WithTheMusicOffAtLaunch_ItNeverStarts()
        {
            SetVolumes(effects: 0.8f, music: 0);

            music.Follow();
            music.Follow();

            Assert.That(output.Calls, Is.EqualTo(new[] { "pause music" }));
        }

        // AppRoot follows the switch every frame: an unchanged volume does nothing, and costs nothing.
        [Test]
        public void FollowingAnUnchangedSwitch_DoesNothing_AndAllocatesNoMemory()
        {
            SetVolumes(effects: 0.8f, music: 0.6f);
            music.Follow();

            Assert.That(() => music.Follow(), Is.Not.AllocatingGCMemory());
            Assert.That(output.Calls, Is.EqualTo(new[] { "music 0.6" }));
        }
    }
}
