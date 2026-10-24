using System;
using Mycelia.Game;
using UnityEngine.SceneManagement;

namespace Mycelia.App
{
    /// <summary>
    /// Changes scenes through a <see cref="SceneTransition"/>: fades out, loads the scene asynchronously, and when it has
    /// loaded (after Awake and OnEnable, before Start) applies its frame rate, calls its entry's
    /// <see cref="ISceneEntry.Construct"/> and fades in. Input is blocked for the whole change.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader, IDisposable
    {
        readonly SceneTransition transition;

        public SceneLoader(IScreenFade fade)
        {
            transition = new SceneTransition(fade, StartLoad);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public bool IsBlockingInput => transition.IsChangingScene;

        public void Load(SceneId scene, AppServices services, SceneArgs args = null) => transition.Request(scene, services, args);

        public void Dispose() => SceneManager.sceneLoaded -= OnSceneLoaded;

        static bool StartLoad(SceneId scene) => SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Single) != null;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => transition.Arrived(scene.name, () => SceneEntries.FindSingle(scene));
    }
}
