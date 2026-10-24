using System;
using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>
    /// The order of a scene change (design §8.3 slice 2): fade out, load once the screen is covered, build the new scene
    /// through its entry, then fade in. Nothing can be pressed in between: a second request while the screen is changing
    /// is ignored (the first wins), except from the new scene's own <see cref="ISceneEntry.Construct"/>, which may open
    /// another scene while the screen is still covered. Engine-free so it can be tested; <see cref="SceneLoader"/>
    /// connects it to Unity's scene loading.
    /// </summary>
    public sealed class SceneTransition
    {
        readonly IScreenFade fade;
        readonly Func<SceneId, bool> startLoad;
        readonly Action covered;
        readonly Action shown;
        SceneId? loading;
        AppServices pendingServices;
        SceneArgs pendingArgs;
        bool constructing;

        /// <param name="startLoad">Starts loading a scene; false if the scene can't be loaded.</param>
        public SceneTransition(IScreenFade fade, Func<SceneId, bool> startLoad)
        {
            this.fade = fade ?? throw new ArgumentNullException(nameof(fade));
            this.startLoad = startLoad ?? throw new ArgumentNullException(nameof(startLoad));
            covered = StartLoading;
            shown = Shown;
        }

        /// <summary>True from a request until the new scene has faded in.</summary>
        public bool IsChangingScene { get; private set; }

        public void Request(SceneId scene, AppServices services, SceneArgs args)
        {
            if (scene == SceneId.Bootstrap)
                throw new ArgumentException("Bootstrap is opened once, by Unity at startup.", nameof(scene));
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (IsChangingScene && !constructing)
                return;

            loading = scene;
            pendingServices = services;
            pendingArgs = args;
            IsChangingScene = true;
            fade.FadeOut(covered);
        }

        /// <summary>A scene has finished loading; builds it if it is the one requested, then fades it in.</summary>
        public void Arrived(string sceneName, Func<ISceneEntry> findEntry)
        {
            if (!loading.HasValue || loading.Value.ToString() != sceneName)
                return;

            SceneId id = loading.Value;
            AppServices services = pendingServices;
            SceneArgs args = pendingArgs;
            Clear();

            FrameRatePolicy.Apply(id);
            constructing = true;
            try
            {
                findEntry().Construct(services, args);
            }
            finally
            {
                constructing = false;
            }

            // The entry opened another scene: stay covered for it.
            if (!loading.HasValue)
                fade.FadeIn(shown);
        }

        void StartLoading()
        {
            SceneId scene = loading.Value;
            if (startLoad(scene))
                return;

            Clear();
            IsChangingScene = false;
            throw new InvalidOperationException($"The {scene} scene is not in the build settings.");
        }

        void Shown() => IsChangingScene = false;

        void Clear()
        {
            loading = null;
            pendingServices = null;
            pendingArgs = null;
        }
    }
}
