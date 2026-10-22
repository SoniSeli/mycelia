using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mycelia.App
{
    /// <summary>Finds a scene's one entry point. None, or more than one, is a setup mistake (ADR-0010).</summary>
    public static class SceneEntries
    {
        public static ISceneEntry FindSingle(Scene scene) => FindSingle(scene.name, scene.GetRootGameObjects());

        public static ISceneEntry FindSingle(string sceneName, IReadOnlyList<GameObject> roots)
        {
            ISceneEntry found = null;
            int count = 0;
            foreach (GameObject root in roots)
            {
                foreach (ISceneEntry entry in root.GetComponentsInChildren<ISceneEntry>(includeInactive: true))
                {
                    found = entry;
                    count++;
                }
            }

            if (count != 1)
                throw new InvalidOperationException($"The {sceneName} scene needs exactly one {nameof(ISceneEntry)}, but has {count}.");
            return found;
        }
    }
}
