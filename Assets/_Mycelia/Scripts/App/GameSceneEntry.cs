using System;
using Mycelia.Content;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// The Game scene's entry point: starts a <see cref="LevelSession"/> on the level it is given, with a fresh seed.
    /// The board view, input and HUD are handed the session here as they arrive (design §8.1).
    /// </summary>
    public sealed class GameSceneEntry : MonoBehaviour, ISceneEntry
    {
        [SerializeField, Tooltip("The level to play when Play is pressed in this scene in the Editor (ADR-0010).")]
        LevelDefinition debugLevel;

        public LevelSession Session { get; private set; }

        public void Construct(AppServices services, SceneArgs args)
        {
            LevelDefinition level = args is GameSceneArgs game ? game.Level : debugLevel;
            if (level == null)
                throw new InvalidOperationException("The Game scene needs a level: open it with GameSceneArgs, or set its Debug Level.");

            LevelConfig config = LevelDefinitionMapper.ToConfig(level, services.Spores);
            ulong seed = services.Seeds.Next();
            if (Debug.isDebugBuild)
                Debug.Log($"[Mycelia] Level '{config.Key}' starts with seed {seed:x16}.");

            Session = new LevelSession(config, new Pcg32(seed));
        }
    }
}
