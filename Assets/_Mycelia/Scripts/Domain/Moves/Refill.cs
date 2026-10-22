using System;
using System.Collections.Generic;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Randomness;

namespace Mycelia.Domain.Moves
{
    /// <summary>
    /// Design §3.3 step 3: every empty playable cell gets a spore chosen uniformly from the level's types.
    /// Also fills the starting board. Draw order is part of the seed contract (ADR-0007):
    /// columns left to right, each from the bottom up.
    /// </summary>
    public static class Refill
    {
        public static void Apply(Board board, IReadOnlyList<SporeTypeId> types, IRandom random, List<SporeSpawned> spawned)
        {
            if (types.Count == 0)
                throw new ArgumentException("A level needs at least one spore type.", nameof(types));

            for (int column = 0; column < board.Width; column++)
            {
                int dropSlot = 0;
                for (int row = 0; row < board.Height; row++)
                {
                    var cell = new GridPosition(column, row);
                    if (!board.IsPlayable(cell) || board.GetOccupant(cell) != SporeTypeId.None)
                        continue;

                    SporeTypeId type = types[random.Next(types.Count)];
                    board.SetOccupant(cell, type);
                    spawned.Add(new SporeSpawned(cell, type, dropSlot));
                    dropSlot++;
                }
            }
        }
    }
}
