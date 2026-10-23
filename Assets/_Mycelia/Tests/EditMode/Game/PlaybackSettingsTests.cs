using NUnit.Framework;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class PlaybackSettingsTests
    {
        PlaybackSettings settings;

        [SetUp]
        public void CreateSettings() => settings = ScriptableObject.CreateInstance<PlaybackSettings>();

        [TearDown]
        public void DestroySettings() => Object.DestroyImmediate(settings);

        [Test]
        public void ALongerFall_TakesLonger_LikeGravity()
        {
            // Under gravity, time grows with the square root of the distance: four cells take twice as long as one.
            Assert.That(settings.FallDuration(4), Is.EqualTo(2 * settings.FallDuration(1)).Within(1e-5f));
            Assert.That(settings.FallDuration(9), Is.EqualTo(3 * settings.FallDuration(1)).Within(1e-5f));
        }

        [Test]
        public void EvenAZeroLengthFall_TakesAsLongAsOneCell()
        {
            Assert.That(settings.FallDuration(0), Is.EqualTo(settings.FallDuration(1)));
        }
    }
}
