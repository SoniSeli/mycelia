using System;
using Mycelia.Content;

namespace Mycelia.App
{
    /// <summary>Opens the Game scene on a level.</summary>
    public sealed class GameSceneArgs : SceneArgs
    {
        public GameSceneArgs(LevelDefinition level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            Level = level;
        }

        public LevelDefinition Level { get; }
    }
}
