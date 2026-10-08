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
        /// <summary>Joins are validated, so only the hardest patterns need a breather afterwards.</summary>
        public int RecoveryRows => Rating >= 0.6f ? 1 : 0;

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
            // Warm-up (only at the very start): one or two obstacles per row.
            new("single side", 0.0f, 0.0f, 1.5f, "J.."),
            new("rock pair", 0.05f, 0.0f, 1.5f, "R.R"),
            new("zigzag", 0.1f, 0.0f, 2f, "J..|..J"),
            new("rock steps", 0.1f, 0.0f, 1.5f, "R..|.R.|..R"),
            new("gate", 0.12f, 0.0f, 2f, "J.J"),
            new("two lanes", 0.15f, 0.0f, 2f, "JJ."),
            new("lonely tower", 0.15f, 0.0f, 2.5f, "T.."),
            new("tower middle", 0.18f, 0.0f, 2.5f, ".T."),

            // Medium: every row demands a move.
            new("full jump", 0.25f, 0.1f, 1.5f, "JJJ"),
            new("rock wall", 0.25f, 0.1f, 1.2f, "RRR"),
            new("double gate", 0.25f, 0.1f, 1.5f, "J.J|J.J"),
            new("corridor", 0.25f, 0.1f, 1.5f, "JJ.|JJ."),
            new("tower slalom", 0.25f, 0.1f, 2f, "T..|...|..T"),
            new("staircase", 0.3f, 0.1f, 1.5f, "J..|.J.|..J"),
            new("tower gate", 0.3f, 0.1f, 2.5f, "T.T"),
            new("tower and rock", 0.3f, 0.1f, 2f, "T.R"),
            new("jump past tower", 0.35f, 0.15f, 2f, "JTJ"),
            new("tower step", 0.35f, 0.15f, 2f, "T..|.T."),
            new("tower behind wall", 0.35f, 0.15f, 1.5f, "..T|J.."),
            new("shifting wall", 0.35f, 0.15f, 1.5f, "JJ.|.JJ"),
            new("gate then middle", 0.35f, 0.15f, 1.5f, "J.J|.J."),
            new("rocks and tower", 0.4f, 0.15f, 2f, "R.R|.T."),
            new("middle then gate", 0.4f, 0.2f, 2f, ".J.|T.T"),
            new("tower then jumps", 0.4f, 0.2f, 2f, "T..|.JJ"),
            new("jump into tower lane", 0.45f, 0.2f, 2f, "JJ.|...|..T"),

            // Hard: chains with a single safe line through them.
            new("weave", 0.5f, 0.25f, 1.5f, "JJ.|J.J|.JJ"),
            new("double jump", 0.5f, 0.25f, 1.2f, "JJJ|...|JJJ"),
            new("tower corridor", 0.5f, 0.25f, 2f, "T.T|.J."),
            new("tower then wall", 0.5f, 0.25f, 1.5f, ".T.|J.J"),
            new("rocks over walls", 0.55f, 0.3f, 1.2f, "RJR|J.J"),
            new("tower ambush", 0.55f, 0.3f, 2f, "JJ.|..T|.J."),
            new("tower sandwich", 0.55f, 0.3f, 2f, "J.J|.T.|J.J"),
            new("switchback", 0.6f, 0.3f, 1.2f, "JJ.|.JJ|JJ."),
            new("tower chicane", 0.6f, 0.35f, 2f, ".JJ|T..|.JJ"),
            new("diagonal towers", 0.6f, 0.35f, 1.8f, "J.T|.J.|T.J"),
            new("tower diamond", 0.65f, 0.35f, 1.8f, "T.J|.J.|J.T"),
            new("triple threat", 0.65f, 0.4f, 1.5f, "JTJ|...|TJT"),
            new("jump then towers", 0.65f, 0.4f, 1.8f, "JJJ|...|T.T"),
            new("tower slingshot", 0.65f, 0.4f, 1.8f, "T..|.JJ|...|..T"),

            // Very hard: late game.
            new("back to back", 0.7f, 0.4f, 1.2f, "JJJ|JJJ"),
            new("tower fortress", 0.7f, 0.45f, 1.5f, "J.J|.T.|JJ."),
            new("jump gauntlet", 0.75f, 0.45f, 1.2f, "JJJ|J.J|JJJ"),
            new("checkerboard", 0.75f, 0.45f, 1.2f, ".J.|J.J|.J.|J.J"),
            new("tower crossfire", 0.75f, 0.5f, 1.5f, "T.T|.J.|J.J"),
            new("towers and walls", 0.8f, 0.5f, 1.2f, "T.J|...|J.T"),
            new("long tower run", 0.8f, 0.5f, 1.2f, "T..|.J.|..J|.J.|T.."),
            new("long weave", 0.85f, 0.55f, 1f, "JJ.|J.J|.JJ|J.J"),
            new("rock maze", 0.85f, 0.55f, 1f, "RR.|.RR|R.R|.R."),
            new("tower alley", 0.85f, 0.6f, 1.2f, "T.T|.J.|...|T.T|.J."),
            new("endless weave", 0.95f, 0.65f, 1f, "JJ.|.JJ|J.J|JJ.|.JJ"),
        };
    }
}
