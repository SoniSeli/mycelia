using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mycelia.App.Tests
{
    /// <summary>The sound files' import settings (performance budget, audio memory ≤ 10 MB; ADR-0017).</summary>
    public class ProjectAudioTests
    {
        const string Folder = "Assets/_Mycelia/Audio";

        static AudioImporter[] Importers() =>
            AssetDatabase.FindAssets("t:AudioClip", new[] { Folder })
                .Select(guid => (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();

        [Test]
        public void TheGame_HasItsEffectsAndOneMusicLoop()
        {
            string[] names = Importers().Select(importer => System.IO.Path.GetFileNameWithoutExtension(importer.assetPath)).ToArray();

            Assert.That(names, Is.EquivalentTo(Enumerable.Range(1, 12).Select(i => $"tick-{i:00}")
                .Concat(new[] { "pop", "tap", "win", "loss", "music-loop" })));
        }

        [Test]
        public void Effects_AreMono_AndDecompressedOnLoad_SoTheyPlayAtOnce()
        {
            foreach (AudioImporter importer in Importers().Where(importer => !importer.assetPath.Contains("music")))
            {
                Assert.That(importer.forceToMono, Is.True, importer.assetPath);
                AudioImporterSampleSettings settings = importer.GetOverrideSampleSettings("Android");
                Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad), importer.assetPath);
                Assert.That(settings.preloadAudioData, Is.True, importer.assetPath);
            }
        }

        [Test]
        public void TheMusic_IsStreamed_AsMonoVorbis()
        {
            AudioImporter music = Importers().Single(importer => importer.assetPath.EndsWith("music-loop.wav"));
            AudioImporterSampleSettings settings = music.GetOverrideSampleSettings("Android");

            Assert.That(music.forceToMono, Is.True);
            Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis));
        }
    }
}
