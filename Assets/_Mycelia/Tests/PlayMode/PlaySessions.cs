using System;
using System.Collections.Generic;
using System.Linq;
using Mycelia.Content;
using Mycelia.Domain;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Levels;
using Mycelia.Domain.Randomness;
using Mycelia.Domain.Sessions;

namespace Mycelia.Game.PlayTests
{
    /// <summary>Sessions on exact boards written as text grids, top row first; letters A–E are spore types 1–5.</summary>
    static class PlaySessions
    {
        public static SporeTypeId IdOf(char letter) => new SporeTypeId((byte)(letter - 'A' + 1));

        public static LevelSession Start(string grid, IRandom random, int moves = 10, int collectA = 5000)
        {
            string[] rows = grid.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(row => row.Replace(" ", "").Trim())
                .Where(row => row.Length > 0)
                .ToArray();
            var board = new Board(rows[0].Length, rows.Length);
            for (int i = 0; i < rows.Length; i++)
            for (int column = 0; column < rows[i].Length; column++)
                board.SetOccupant(new GridPosition(column, rows.Length - 1 - i), IdOf(rows[i][column]));

            return new LevelSession(Config(board.Width, board.Height, moves, collectA), random, board);
        }

        public static LevelSession StartRandom(int width, int height, IRandom random, int moves) =>
            new LevelSession(Config(width, height, moves, 5000), random);

        // Spore types are the project catalog's four, so A–D look like real spores. The goal is to collect A spores.
        static LevelConfig Config(int width, int height, int moves, int collectA) => new LevelConfig(
            "level-playback", width, height, moves,
            "ABCD".Select(IdOf).ToArray(),
            new[] { new ObjectiveDefinition(IdOf('A'), collectA) });

        public static SporeCatalog ProjectSpores
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.FindAssets("t:SporeCatalog")
                    .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<SporeCatalog>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                    .Single();
#else
                throw new NotSupportedException("Playback tests run in the Editor.");
#endif
            }
        }
    }

    /// <summary>Returns preset values in order, so a test controls refills and shuffles exactly (ADR-0007).</summary>
    sealed class ScriptedRandom : IRandom
    {
        readonly Queue<int> values;

        public ScriptedRandom(params int[] values) => this.values = new Queue<int>(values);

        public int Next(int maxExclusive)
        {
            if (values.Count == 0)
                throw new InvalidOperationException("ScriptedRandom ran out of values.");
            int value = values.Dequeue();
            if (value < 0 || value >= maxExclusive)
                throw new InvalidOperationException($"Scripted value {value} is outside [0, {maxExclusive}).");
            return value;
        }
    }
}
