using System;
using System.IO;

namespace Mycelia.App
{
    /// <summary>
    /// Where the app goes after Bootstrap. A build opens the Game scene with the start level, until Level Select
    /// exists (design §8.1). In the Editor, Play always boots through Bootstrap and then reopens the scene Play was
    /// pressed in, with that scene's own debug args (ADR-0010).
    /// </summary>
    public readonly struct StartScene
    {
        /// <summary>The Editor's SessionState key for the path of the scene Play was pressed in.</summary>
        public const string EditorSceneKey = "Mycelia.App.EditorStartScene";

        StartScene(SceneId scene, SceneArgs args)
        {
            Scene = scene;
            Args = args;
        }

        public SceneId Scene { get; }

        /// <summary>Null when the scene should use its own debug args.</summary>
        public SceneArgs Args { get; }

        /// <param name="editorScenePath">The scene Play was pressed in, or null in a build.</param>
        public static StartScene Choose(GameSceneArgs startLevel, string editorScenePath)
        {
            if (TryFindScene(editorScenePath, out SceneId scene) && scene != SceneId.Bootstrap)
                return new StartScene(scene, null);

            return new StartScene(SceneId.Game, startLevel);
        }

        static bool TryFindScene(string path, out SceneId scene)
        {
            string name = Path.GetFileNameWithoutExtension(path ?? "");
            foreach (SceneId id in (SceneId[])Enum.GetValues(typeof(SceneId)))
            {
                if (id.ToString() == name)
                {
                    scene = id;
                    return true;
                }
            }

            scene = default;
            return false;
        }
    }
}
