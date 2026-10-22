using System;
using UnityEngine.SceneManagement;

namespace Mycelia.App
{
    /// <summary>
    /// Loads scenes asynchronously, one at a time. When the scene has loaded (after Awake and OnEnable, before Start)
    /// it applies the scene's frame rate and calls its entry's <see cref="ISceneEntry.Construct"/>.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader, IDisposable
    {
        SceneId? loading;
        AppServices pendingServices;
        SceneArgs pendingArgs;

        public SceneLoader() => SceneManager.sceneLoaded += OnSceneLoaded;

        public void Load(SceneId scene, AppServices services, SceneArgs args = null)
        {
            if (scene == SceneId.Bootstrap)
                throw new ArgumentException("Bootstrap is opened once, by Unity at startup.", nameof(scene));
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (loading.HasValue)
                throw new InvalidOperationException($"Can't open {scene} while {loading.Value} is still loading.");

            loading = scene;
            pendingServices = services;
            pendingArgs = args;
            if (SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Single) == null)
            {
                Clear();
                throw new InvalidOperationException($"The {scene} scene is not in the build settings.");
            }
        }

        public void Dispose() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!loading.HasValue || scene.name != loading.Value.ToString())
                return;

            SceneId id = loading.Value;
            AppServices services = pendingServices;
            SceneArgs args = pendingArgs;
            // Cleared first, so the entry may open another scene from Construct.
            Clear();

            FrameRatePolicy.Apply(id);
            SceneEntries.FindSingle(scene).Construct(services, args);
        }

        void Clear()
        {
            loading = null;
            pendingServices = null;
            pendingArgs = null;
        }
    }
}
