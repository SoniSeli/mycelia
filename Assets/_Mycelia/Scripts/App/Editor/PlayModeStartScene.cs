using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mycelia.App.Editor
{
    /// <summary>
    /// Makes Play boot through Bootstrap from any saved scene, so <see cref="AppRoot"/> always creates the services
    /// (ADR-0010). The scene Play was pressed in is passed on to AppRoot, which reopens it with its debug args.
    /// </summary>
    [InitializeOnLoad]
    static class PlayModeStartScene
    {
        static PlayModeStartScene() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        /// <summary>
        /// Saved scenes boot through Bootstrap. An untitled scratch scene plays as it is, and so does the Test Runner's
        /// own play-mode scene, saved as Assets/InitTestScene&lt;guid&gt;.unity, so PlayMode tests run their own setup.
        /// </summary>
        internal static bool BootsThroughBootstrap(string activeScenePath) =>
            !string.IsNullOrEmpty(activeScenePath) && !activeScenePath.StartsWith("Assets/InitTestScene");

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                Redirect();
            else if (change == PlayModeStateChange.EnteredEditMode)
                EditorSceneManager.playModeStartScene = null;
        }

        static void Redirect()
        {
            EditorSceneManager.playModeStartScene = null;
            SessionState.EraseString(StartScene.EditorSceneKey);

            string activeScenePath = SceneManager.GetActiveScene().path;
            if (!BootsThroughBootstrap(activeScenePath))
                return;

            // Bootstrap loads the scene from disk, so unsaved edits would silently be missing from Play.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            SceneAsset bootstrap = buildScenes.Length > 0 ? AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScenes[0].path) : null;
            if (bootstrap == null)
            {
                Debug.LogError("[Mycelia] Bootstrap must be the first scene in the build settings; playing the open scene as it is.");
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrap;
            SessionState.SetString(StartScene.EditorSceneKey, activeScenePath);
        }
    }
}
