using System;
using System.Collections.Generic;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Randomness;

namespace Mycelia.Domain.Moves
{
    public enum ReshuffleOutcome
    {
        /// <summary>A valid chain already existed; the board is unchanged.</summary>
        NotNeeded,

        /// <summary>The existing spores were rearranged, keeping type counts.</summary>
        Shuffled,

        /// <summary>No shuffle worked, so the board was cleared and refilled from the level's types.</summary>
        Regenerated
    }

    /// <summary>
    /// Design §3.3 step 5: guarantees the player always has a move. A dead board is shuffled
    /// (Fisher–Yates over the ADR-0007 cell order, type counts kept) and, if that fails, regenerated.
    /// A reshuffle costs no move. Buffers are sized once per board, so applying it does not allocate.
    /// </summary>
    public sealed class Reshuffler
    {
        public const int MaxShuffleAttempts = 10;
        public const int MaxRegenerateAttempts = 10;

        readonly Board board;
        readonly GridPosition[] cells;
        readonly SporeTypeId[] originals;
        readonly int[] order;
        int count;

        public Reshuffler(Board board)
        {
            this.board = board;
            int capacity = board.Width * board.Height;
            cells = new GridPosition[capacity];
            originals = new SporeTypeId[capacity];
            order = new int[capacity];
        }

        public ReshuffleOutcome Apply(
            IReadOnlyList<SporeTypeId> types,
            IRandom random,
            List<SporeRelocated> relocated,
            List<SporeCleared> cleared,
            List<SporeSpawned> spawned)
        {
            if (ValidChains.Exists(board))
                return ReshuffleOutcome.NotNeeded;

            if (!ValidChains.ShapeAllowsAChain(board))
                throw new InvalidOperationException("No two playable cells on this board are neighbours, so a chain is impossible.");

            CollectSpores();

            for (int attempt = 0; attempt < MaxShuffleAttempts; attempt++)
            {
                Shuffle(random);
                if (ValidChains.Exists(board))
                {
                    RecordRelocations(relocated);
                    return ReshuffleOutcome.Shuffled;
                }
            }

            Regenerate(types, random, cleared, spawned);
            return ReshuffleOutcome.Regenerated;
        }

        void CollectSpores()
        {
            count = 0;
            for (int column = 0; column < board.Width; column++)
            {
                for (int row = 0; row < board.Height; row++)
                {
                    var cell = new GridPosition(column, row);
                    if (!board.IsPlayable(cell) || board.GetOccupant(cell) == SporeTypeId.None)
                        continue;

                    cells[count] = cell;
                    originals[count] = board.GetOccupant(cell);
                    count++;
                }
            }
        }

        /// <summary>Fisher–Yates from the last cell down; cell i receives the spore that started at cell order[i].</summary>
        void Shuffle(IRandom random)
        {
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }

            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            for (int i = 0; i < count; i++)
            {
                board.SetOccupant(cells[i], originals[order[i]]);
            }
        }

        void RecordRelocations(List<SporeRelocated> relocated)
        {
            for (int i = 0; i < count; i++)
            {
                if (order[i] != i)
                    relocated.Add(new SporeRelocated(cells[order[i]], cells[i]));
            }
        }

        void Regenerate(IReadOnlyList<SporeTypeId> types, IRandom random, List<SporeCleared> cleared, List<SporeSpawned> spawned)
        {
            // The view only ever saw the original spores, so those are what gets cleared.
            for (int i = 0; i < count; i++)
            {
                cleared.Add(new SporeCleared(cells[i], originals[i]));
            }

            int firstSpawn = spawned.Count;
            for (int attempt = 0; attempt < MaxRegenerateAttempts; attempt++)
            {
                EmptyAllPlayableCells();
                spawned.RemoveRange(firstSpawn, spawned.Count - firstSpawn);
                Refill.Apply(board, types, random, spawned);
                if (ValidChains.Exists(board))
                    return;
            }

            throw new InvalidOperationException(
                $"Could not generate a board with a valid chain in {MaxRegenerateAttempts} attempts; check the level's spore types.");
        }

        void EmptyAllPlayableCells()
        {
            for (int column = 0; column < board.Width; column++)
            {
                for (int row = 0; row < board.Height; row++)
                {
                    var cell = new GridPosition(column, row);
                    if (board.IsPlayable(cell))
                        board.SetOccupant(cell, SporeTypeId.None);
                }
            }
        }
    }
}
