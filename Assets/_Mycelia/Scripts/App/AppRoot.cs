using Mycelia.Content;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The composition root (ADR-0010). Lives in the Bootstrap scene, creates every service once, and opens the first
    /// scene. It survives scene loads but is never looked up: services reach scenes only through their entries.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField] SporeCatalog spores;

        [SerializeField] LevelCatalog levels;

        SceneLoader scenes;
        PlayerProgress progress;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            FrameRatePolicy.Apply(SceneId.Bootstrap);

            progress = PlayerProgress.Load(new JsonProgressStore(Application.persistentDataPath), levels.Keys);
            if (Debug.isDebugBuild)
                Debug.Log($"[Mycelia] Progress: {progress.LoadOutcome}, {progress.Levels.Records.Count} level record(s).");

            scenes = new SceneLoader();
            var services = new AppServices(
                scenes: scenes,
                seeds: new CryptoSeedSource(),
                spores: spores,
                levels: levels,
                progress: progress);

            StartScene start = StartScene.Choose(TakeEditorStartScene());
            scenes.Load(start.Scene, services, start.Args);
        }

        // ADR-0006: the OS may suspend or kill the app right after this, so the write is synchronous. It also catches
        // a win whose move was still playing when the app went to the background.
        void OnApplicationPause(bool paused)
        {
            if (paused)
                progress?.SaveIfChanged();
        }

        void OnDestroy() => scenes?.Dispose();

        // The Editor leaves the scene Play was pressed in (PlayModeStartScene); a build has none.
        static string TakeEditorStartScene()
        {
#if UNITY_EDITOR
            string path = UnityEditor.SessionState.GetString(StartScene.EditorSceneKey, null);
            UnityEditor.SessionState.EraseString(StartScene.EditorSceneKey);
            return path;
#else
            return null;
#endif
        }
    }
}
