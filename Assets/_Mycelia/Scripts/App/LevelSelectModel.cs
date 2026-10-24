using System.Collections.Generic;
using Mycelia.Domain.Levels;
using Mycelia.Game;

namespace Mycelia.App
{
    /// <summary>Turns the catalog's level order and the player's progress into what each level select button shows.</summary>
    public static class LevelSelectModel
    {
        public static LevelButtonState[] For(IReadOnlyList<string> levelOrder, Progression progression)
        {
            var levels = new LevelButtonState[levelOrder.Count];
            for (int i = 0; i < levelOrder.Count; i++)
            {
                string key = levelOrder[i];
                levels[i] = new LevelButtonState(i + 1, progression.IsUnlocked(key), progression.BestStars(key));
            }
            return levels;
        }
    }
}
