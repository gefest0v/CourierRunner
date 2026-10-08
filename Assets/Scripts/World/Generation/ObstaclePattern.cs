using System;
using System.Collections.Generic;

namespace CourierRunner
{
    /// <summary>What occupies one lane of one pattern row.</summary>
    public enum PatternCell
    {
        Empty,
        /// <summary>Low obstacle the courier can jump (wall or rock, chosen at random).</summary>
        Jump,
        /// <summary>Low wooden wall, jumpable.</summary>
        Wall,
        /// <summary>Low rock, jumpable.</summary>
        Rock,
        /// <summary>Tower: blocks the lane and shoots down it. Cannot be jumped.</summary>
        Tower,
    }

    /// <summary>
    /// A hand-made, pre-checked arrangement of obstacles. Rows are listed in the order the courier
    /// reaches them, each row is three characters for the left, middle and right lane:
    /// <c>.</c> empty, <c>J</c> any jumpable obstacle, <c>W</c> wall, <c>R</c> rock, <c>T</c> tower.
    /// </summary>
    public sealed class ObstaclePattern
    {
        public readonly string Name;
        /// <summary>0 = trivial, 1 = hardest. Patterns near the current difficulty are preferred.</summary>
        public readonly float Rating;
        public readonly float MinDifficulty;
        public readonly float Weight;
        public readonly PatternCell[][] Rows;

        public bool HasTower { get; }
        /// <summary>Empty rows the generator leaves after this pattern.</summary>
        public int RecoveryRows => Rating >= 0.5f ? 2 : 1;

        public ObstaclePattern(string name, float rating, float minDifficulty, float weight, string rows)
        {
            Name = name;
            Rating = rating;
            MinDifficulty = minDifficulty;
            Weight = weight;
            string[] parts = rows.Split('|');
            Rows = new PatternCell[parts.Length][];
            for (int r = 0; r < parts.Length; r++)
            {
                string row = parts[r].Trim();
                if (row.Length != 3) throw new ArgumentException($"Pattern '{name}': row '{row}' must have 3 lanes.");
                Rows[r] = new PatternCell[3];
                for (int lane = 0; lane < 3; lane++)
                {
                    Rows[r][lane] = Parse(row[lane], name);
                    if (Rows[r][lane] == PatternCell.Tower) HasTower = true;
                }
            }
        }

        private static PatternCell Parse(char symbol, string name) => symbol switch
        {
            '.' => PatternCell.Empty,
            'J' => PatternCell.Jump,
            'W' => PatternCell.Wall,
            'R' => PatternCell.Rock,
            'T' => PatternCell.Tower,
            _ => throw new ArgumentException($"Pattern '{name}': unknown cell '{symbol}'."),
        };

        public static bool IsJumpable(PatternCell cell) =>
            cell == PatternCell.Jump || cell == PatternCell.Wall || cell == PatternCell.Rock;

        public override string ToString() => Name;
    }

    /// <summary>The verified pattern set. Every pattern is mirrored at random, so only one side is listed.</summary>
    public static class PatternLibrary
    {
        public static readonly IReadOnlyList<ObstaclePattern> All = new List<ObstaclePattern>
        {
            // Warm-up: one obstacle, two free lanes.
            new("single side", 0.0f, 0.0f, 3f, "J.."),
            new("single middle", 0.0f, 0.0f, 2f, ".J."),
            new("rock pair", 0.05f, 0.0f, 2f, "R.R"),
            new("zigzag", 0.15f, 0.0f, 2f, "J..|..J"),
            new("two lanes", 0.2f, 0.05f, 2f, "JJ."),
            new("gate", 0.2f, 0.05f, 2f, "J.J"),

            // Medium: need a lane change or a jump in most rows.
            new("full jump", 0.3f, 0.15f, 1.5f, "JJJ"),
            new("staircase", 0.35f, 0.15f, 1.5f, "J..|.J.|..J"),
            new("shifting wall", 0.4f, 0.2f, 1.5f, "JJ.|.JJ"),
            new("gate then middle", 0.4f, 0.2f, 1.5f, "J.J|.J."),
            new("lonely tower", 0.3f, 0.15f, 2f, "T.."),
            new("tower middle", 0.35f, 0.2f, 1.5f, ".T."),
            new("tower and rock", 0.45f, 0.25f, 1.5f, "T.R"),

            // Hard: chains with one safe line through them.
            new("weave", 0.55f, 0.35f, 1.2f, "JJ.|J.J|.JJ"),
            new("double jump", 0.55f, 0.35f, 1.2f, "JJJ|...|JJJ"),
            new("tower corridor", 0.6f, 0.4f, 1.2f, "T.T|.J."),
            new("tower then wall", 0.6f, 0.4f, 1.2f, ".T.|J.J"),
            new("switchback", 0.65f, 0.45f, 1f, "JJ.|.JJ|JJ."),

            // Very hard: late game only.
            new("jump gauntlet", 0.8f, 0.6f, 1f, "JJJ|J.J|JJJ"),
            new("towers and walls", 0.85f, 0.65f, 1f, "T.J|...|J.T"),
            new("long weave", 0.9f, 0.7f, 0.8f, "JJ.|J.J|.JJ|J.J"),
        };
    }
}
