using System.Collections.Generic;
using Mycelia.Domain;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Objectives;
using Mycelia.Domain.Sessions;

namespace Mycelia.Balancing
{
    /// <summary>
    /// A simple player for balancing (design §8.2 slice 7): it takes the longest chain of a spore type a goal still
    /// needs, or the longest chain of any type when none can be made. It plans one move at a time and never looks
    /// ahead, so it is a steady, slightly weak player: its win rates rank levels by difficulty rather than predict
    /// a person's.
    /// </summary>
    public sealed class GreedyBot
    {
        // Longest-path search is exponential in the worst case; this many steps per group is plenty on boards of
        // up to 8 × 8, and the best path found so far is used if it runs out.
        const int StepBudget = 20000;

        static readonly (int Column, int Row)[] Directions =
            { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1) };

        int steps;

        /// <summary>The cells of the chosen chain, in drag order; empty if the board has no chain at all.</summary>
        public GridPosition[] ChooseChain(LevelSession session)
        {
            IReadOnlyBoard board = session.Board;
            var needed = new HashSet<SporeTypeId>();
            foreach (IObjective objective in session.Objectives)
            {
                if (objective is CollectSporesObjective collect && !collect.IsComplete)
                    needed.Add(collect.Type);
            }

            var seen = new bool[board.Width, board.Height];
            List<GridPosition> best = null;
            bool bestNeeded = false;
            for (int column = 0; column < board.Width; column++)
            for (int row = 0; row < board.Height; row++)
            {
                if (seen[column, row])
                    continue;
                var cell = new GridPosition(column, row);
                List<GridPosition> group = Group(board, cell, seen);
                if (group.Count < 2)
                    continue;

                bool isNeeded = needed.Contains(board.GetOccupant(cell));
                if (best != null && (bestNeeded && !isNeeded || (bestNeeded == isNeeded && group.Count <= best.Count)))
                    continue;

                List<GridPosition> path = LongestPath(board, group);
                if (best == null || (isNeeded && !bestNeeded) || path.Count > best.Count)
                {
                    best = path;
                    bestNeeded = isNeeded;
                }
            }

            return best != null ? best.ToArray() : new GridPosition[0];
        }

        // Every cell 8-way connected to start through spores of its type.
        static List<GridPosition> Group(IReadOnlyBoard board, GridPosition start, bool[,] seen)
        {
            var group = new List<GridPosition>();
            seen[start.Column, start.Row] = true;
            if (!board.IsPlayable(start))
                return group;
            SporeTypeId type = board.GetOccupant(start);
            if (type == SporeTypeId.None)
                return group;

            var stack = new Stack<GridPosition>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                GridPosition cell = stack.Pop();
                group.Add(cell);
                foreach (GridPosition next in Neighbours(board, cell, type))
                {
                    if (seen[next.Column, next.Row])
                        continue;
                    seen[next.Column, next.Row] = true;
                    stack.Push(next);
                }
            }
            return group;
        }

        List<GridPosition> LongestPath(IReadOnlyBoard board, List<GridPosition> group)
        {
            SporeTypeId type = board.GetOccupant(group[0]);
            var best = new List<GridPosition>();
            var path = new List<GridPosition>();
            var used = new HashSet<GridPosition>();
            steps = 0;

            // Ends of a long path tend to be cells with few neighbours, so start there first.
            group.Sort((a, b) => Degree(board, a, type).CompareTo(Degree(board, b, type)));
            foreach (GridPosition start in group)
            {
                path.Add(start);
                used.Add(start);
                Extend(board, type, path, used, best, group.Count);
                path.Clear();
                used.Clear();
                if (best.Count == group.Count || steps >= StepBudget)
                    break;
            }
            return best;
        }

        void Extend(IReadOnlyBoard board, SporeTypeId type, List<GridPosition> path, HashSet<GridPosition> used,
            List<GridPosition> best, int groupSize)
        {
            if (path.Count > best.Count)
            {
                best.Clear();
                best.AddRange(path);
            }
            if (best.Count == groupSize || ++steps >= StepBudget)
                return;

            var options = new List<GridPosition>();
            foreach (GridPosition next in Neighbours(board, path[path.Count - 1], type))
            {
                if (!used.Contains(next))
                    options.Add(next);
            }
            // Visit the most constrained neighbour first (Warnsdorff's rule).
            options.Sort((a, b) => Degree(board, a, type, used).CompareTo(Degree(board, b, type, used)));

            foreach (GridPosition next in options)
            {
                path.Add(next);
                used.Add(next);
                Extend(board, type, path, used, best, groupSize);
                path.RemoveAt(path.Count - 1);
                used.Remove(next);
                if (best.Count == groupSize || steps >= StepBudget)
                    return;
            }
        }

        static int Degree(IReadOnlyBoard board, GridPosition cell, SporeTypeId type, HashSet<GridPosition> used = null)
        {
            int count = 0;
            foreach (GridPosition next in Neighbours(board, cell, type))
            {
                if (used == null || !used.Contains(next))
                    count++;
            }
            return count;
        }

        static IEnumerable<GridPosition> Neighbours(IReadOnlyBoard board, GridPosition cell, SporeTypeId type)
        {
            foreach (var (dc, dr) in Directions)
            {
                var next = new GridPosition(cell.Column + dc, cell.Row + dr);
                if (next.Column >= 0 && next.Row >= 0 && next.Column < board.Width && next.Row < board.Height
                    && board.IsPlayable(next) && board.GetOccupant(next) == type)
                    yield return next;
            }
        }
    }
}
