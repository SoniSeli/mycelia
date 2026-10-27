using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Mycelia.App.Tests
{
    /// <summary>Guards assets that would ship unused and cost size and memory (performance budget, Milestone 5 slice 1).</summary>
    public class ProjectBuildSizeTests
    {
        // No camera uses post-processing. A renderer with post-process data ships URP's film grain and SMAA textures and
        // its post-processing shaders, and sets them up at launch: about 2 MiB of APK, and most of the 19 MB of memory
        // measured with the other trims.
        [Test]
        public void EveryRenderer_LeavesOutPostProcessData()
        {
            string[] paths = AssetDatabase.FindAssets("t:ScriptableRendererData", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(paths, Is.Not.Empty);
            foreach (string path in paths)
            {
                var renderer = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
                Assert.That(renderer.FindProperty("m_PostProcessData").objectReferenceValue, Is.Null, path);
            }
        }

        // Everything in a Resources folder ships whether or not anything uses it: TMP's LiberationSans did, for 0.5 MiB.
        // Fonts reach the build through the texts and TMP Settings that use them.
        [Test]
        public void NoFontAsset_LivesInAResourcesFolder()
        {
            string[] inResources = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Contains("/Resources/"))
                .ToArray();
            Assert.That(inResources, Is.Empty);
        }
    }
}
