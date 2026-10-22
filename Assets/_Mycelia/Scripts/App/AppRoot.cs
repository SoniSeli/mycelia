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

        [SerializeField, Tooltip("The level a build opens, until Level Select exists (design §8.1).")]
        LevelDefinition startLevel;

        SceneLoader scenes;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            FrameRatePolicy.Apply(SceneId.Bootstrap);

            scenes = new SceneLoader();
            var services = new AppServices(
                scenes: scenes,
                seeds: new CryptoSeedSource(),
                spores: spores);

            StartScene start = StartScene.Choose(new GameSceneArgs(startLevel), TakeEditorStartScene());
            scenes.Load(start.Scene, services, start.Args);
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
