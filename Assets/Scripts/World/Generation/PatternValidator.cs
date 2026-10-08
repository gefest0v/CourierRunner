using System.Collections.Generic;

namespace CourierRunner
{
    /// <summary>Physical limits used to decide whether a pattern can be passed.</summary>
    public struct PassabilityRules
    {
        /// <summary>Distance between pattern rows in meters.</summary>
        public float RowSpacing;
        public float RunSpeed;
        public float LaneChangeSpeed;
        public float LaneWidth;
        /// <summary>Ground distance covered by one jump; constant at every speed.</summary>
        public float JumpLength;
        /// <summary>A tower fires at a courier in its lane closer than this.</summary>
        public float TowerRange;

        public static PassabilityRules From(GameConfig config, float rowSpacing, float speed) => new()
        {
            RowSpacing = rowSpacing,
            RunSpeed = speed,
            LaneChangeSpeed = config.laneChangeSpeed,
            LaneWidth = config.laneWidth,
            JumpLength = config.jumpLength,
            TowerRange = 20f,
        };

        /// <summary>How many lanes the courier can move between two consecutive rows.</summary>
        public int LaneReach
        {
            get
            {
                float timeBetweenRows = RowSpacing / RunSpeed;
                float timePerLane = LaneWidth / LaneChangeSpeed;
                // Allow a little reaction time before the courier starts to move.
                return (int)((timeBetweenRows - 0.15f) / timePerLane);
            }
        }
    }

    /// <summary>One safe way through a pattern: the lane taken at each row and whether it is jumped.</summary>
    public sealed class PatternPath
    {
        public readonly int[] Lanes;
        public readonly bool[] Jumps;

        public PatternPath(int rows)
        {
            Lanes = new int[rows];
            Jumps = new bool[rows];
        }
    }

    /// <summary>
    /// Proves that a pattern is passable under given speed and spacing by searching for a path,
    /// and checks the placement rules from GAME_DESIGN.md §4 (towers never shoot through obstacles).
    /// </summary>
    public static class PatternValidator
    {
        private struct State
        {
            public int Lane;
            public float GroundedFrom; // Track distance from which the courier can jump again.
            public int Parent;
            public bool Jumped;
        }

        /// <param name="rows">Rows in approach order; lanes are indexed 0..2 (left to right).</param>
        /// <param name="entryLane">Lane the courier is in before the first row, or -1 for any lane.</param>
        public static bool TryFindPath(PatternCell[][] rows, PassabilityRules rules, int entryLane, out PatternPath path, out string error)
        {
            path = null;
            if (!TowersHaveClearLanes(rows, rules, out error)) return false;

            int count = rows.Length;
            bool[,] danger = TowerDanger(rows, rules);
            int reach = rules.LaneReach;
            if (reach < 1)
            {
                error = $"rows {rules.RowSpacing:0.#} m apart are too close to change lanes at {rules.RunSpeed:0.#} m/s";
                return false;
            }

            var states = new List<State>();
            var frontier = new List<int>();
            var next = new List<int>();
            // Row -1: the courier may enter in any lane (or the given one), on the ground.
            for (int lane = 0; lane < 3; lane++)
            {
                if (entryLane >= 0 && lane != entryLane) continue;
                states.Add(new State { Lane = lane, GroundedFrom = float.NegativeInfinity, Parent = -1 });
                frontier.Add(states.Count - 1);
            }

            var best = new int[3];
            for (int row = 0; row < count; row++)
            {
                next.Clear();
                float z = row * rules.RowSpacing;
                for (int lane = 0; lane < 3; lane++) best[lane] = -1;
                foreach (int index in frontier)
                {
                    State from = states[index];
                    // Without a previous row the courier has had all the time it needs.
                    int laneReach = row == 0 ? 2 : reach;
                    for (int lane = 0; lane < 3; lane++)
                    {
                        if (System.Math.Abs(lane - from.Lane) > laneReach) continue;
                        if (danger[row, lane]) continue;
                        PatternCell cell = rows[row][lane];
                        if (cell == PatternCell.Tower) continue;

                        State candidate;
                        if (ObstaclePattern.IsJumpable(cell))
                        {
                            // Take off half a jump before the obstacle; that point must be on the ground.
                            float takeOff = z - rules.JumpLength * 0.5f;
                            if (takeOff < from.GroundedFrom) continue;
                            candidate = new State
                            {
                                Lane = lane, GroundedFrom = z + rules.JumpLength * 0.5f, Parent = index, Jumped = true,
                            };
                        }
                        else
                        {
                            candidate = new State { Lane = lane, GroundedFrom = from.GroundedFrom, Parent = index };
                        }

                        // Keep only the state per lane that lands soonest; it dominates the others.
                        if (best[lane] >= 0 && states[best[lane]].GroundedFrom <= candidate.GroundedFrom) continue;
                        states.Add(candidate);
                        best[lane] = states.Count - 1;
                    }
                }
                for (int lane = 0; lane < 3; lane++)
                    if (best[lane] >= 0) next.Add(best[lane]);
                if (next.Count == 0)
                {
                    error = $"no safe lane at row {row + 1}";
                    return false;
                }
                (frontier, next) = (next, frontier);
            }

            path = new PatternPath(count);
            int cursor = frontier[0];
            for (int row = count - 1; row >= 0; row--)
            {
                State state = states[cursor];
                path.Lanes[row] = state.Lane;
                path.Jumps[row] = state.Jumped;
                cursor = state.Parent;
            }
            error = null;
            return true;
        }

        /// <summary>A tower's fireball must never fly through another obstacle on its way to the courier.</summary>
        private static bool TowersHaveClearLanes(PatternCell[][] rows, PassabilityRules rules, out string error)
        {
            for (int row = 0; row < rows.Length; row++)
                for (int lane = 0; lane < 3; lane++)
                {
                    if (rows[row][lane] != PatternCell.Tower) continue;
                    for (int before = row - 1; before >= 0; before--)
                    {
                        if ((row - before) * rules.RowSpacing > rules.TowerRange) break;
                        if (rows[before][lane] != PatternCell.Empty)
                        {
                            error = $"tower at row {row + 1} would shoot through an obstacle at row {before + 1}";
                            return false;
                        }
                    }
                }
            error = null;
            return true;
        }

        /// <summary>Cells inside a tower's firing range, in front of the tower and in its lane.</summary>
        private static bool[,] TowerDanger(PatternCell[][] rows, PassabilityRules rules)
        {
            var danger = new bool[rows.Length, 3];
            for (int row = 0; row < rows.Length; row++)
                for (int lane = 0; lane < 3; lane++)
                {
                    if (rows[row][lane] != PatternCell.Tower) continue;
                    for (int before = row; before >= 0; before--)
                    {
                        if ((row - before) * rules.RowSpacing > rules.TowerRange) break;
                        danger[before, lane] = true;
                    }
                }
            return danger;
        }
    }
}
