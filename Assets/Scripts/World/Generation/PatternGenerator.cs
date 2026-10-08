using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Lays out obstacles and coins as a continuous stream of rows across segments. Every obstacle
    /// combination comes from <see cref="PatternLibrary"/> and is checked by
    /// <see cref="PatternValidator"/> at the current speed and spacing before it is used; coins follow
    /// the safe path the validator found. A pattern is always surrounded by empty recovery rows.
    /// </summary>
    public sealed class PatternGenerator : ISegmentPopulator
    {
        private struct PlannedRow
        {
            public PatternCell[] Cells;
            public float SpacingAfter;
            public bool Coins;
            public int CoinLane;
            public bool CoinArc;
            public bool PowerUp;
        }

        private const float CoinGroundHeight = 1f;
        private const float IntroRowSpacing = 12f;

        private readonly GameConfig config;
        private readonly DifficultyDirector director;
        private readonly Queue<PlannedRow> pending = new();
        private readonly List<ObstaclePattern> candidates = new();
        /// <summary>The last rows already planned; new patterns are validated together with them.</summary>
        private readonly List<PatternCell[]> history = new();
        private const int HistoryRows = 2;
        private static readonly PatternCell[] EmptyRow = new PatternCell[3];

        private System.Random random;
        private float nextRowZ;
        private int recoveryRows;
        private int coinLane;

        /// <summary>Name of the most recently planned pattern, for the debug overlay.</summary>
        public string LastPattern { get; private set; }

        /// <summary>Track distances where obstacles must not appear (e.g. a flight's landing window).</summary>
        public System.Func<float, bool> SuppressObstacles;

        private float lastPowerUpZ;

        public PatternGenerator(GameConfig config, DifficultyDirector director)
        {
            this.config = config;
            this.director = director;
        }

        public void BeginRun(RunMode mode, int seed)
        {
            random = new System.Random(seed);
            pending.Clear();
            history.Clear();
            nextRowZ = IntroRowSpacing * 1.5f;
            recoveryRows = 1;
            coinLane = 1;
            LastPattern = null;
            lastPowerUpZ = 0f;
        }

        public void Populate(SegmentBuildContext context)
        {
            TrackSegment segment = context.Segment;
            while (nextRowZ < segment.EndZ)
            {
                if (nextRowZ >= context.StopZ)
                {
                    pending.Clear();
                    return;
                }
                if (pending.Count == 0) PlanFrom(nextRowZ);
                PlannedRow row = pending.Dequeue();
                // Event segments (treants) keep the road clear; skipping obstacles never breaks a path.
                if (nextRowZ >= segment.StartZ && segment.Profile.allowsGeneratedRows)
                    EmitRow(context, row, nextRowZ - segment.StartZ);
                nextRowZ += row.SpacingAfter;
            }
        }

        private void PlanFrom(float z)
        {
            if (z < config.safeStartDistance)
            {
                // Warm-up: a few coin lines on an empty road.
                Enqueue(new PlannedRow { Cells = EmptyRow, SpacingAfter = IntroRowSpacing, Coins = true, CoinLane = coinLane });
                return;
            }

            float difficulty = director.DifficultyAt(z);
            float spacing = director.RowSpacingAt(z);
            if (ShouldPlaceWings(z))
            {
                PlanWings(spacing);
                return;
            }
            int recovery = recoveryRows;
            if (!TryChoosePattern(z, difficulty, spacing, out PatternCell[][] rows, out PatternPath path, out int offset))
            {
                Enqueue(new PlannedRow { Cells = EmptyRow, SpacingAfter = spacing, Coins = true, CoinLane = coinLane });
                return;
            }

            // The path covers the previous rows, the recovery rows and the new pattern; coins follow it.
            for (int i = 0; i < recovery; i++)
                Enqueue(new PlannedRow { Cells = EmptyRow, SpacingAfter = spacing, Coins = random.NextDouble() < 0.7, CoinLane = path.Lanes[offset + i] });
            for (int r = 0; r < rows.Length; r++)
            {
                int index = offset + recovery + r;
                Enqueue(new PlannedRow
                {
                    Cells = rows[r],
                    SpacingAfter = spacing,
                    Coins = random.NextDouble() < 0.9,
                    CoinLane = path.Lanes[index],
                    CoinArc = path.Jumps[index],
                });
            }
            coinLane = path.Lanes[path.Lanes.Length - 1];
        }

        private void Enqueue(PlannedRow row)
        {
            pending.Enqueue(row);
            history.Add(row.Cells);
            if (history.Count > HistoryRows) history.RemoveAt(0);
        }

        /// <summary>Wings only after they were bought, never too often, and instead of an obstacle pattern.</summary>
        private bool ShouldPlaceWings(float z)
        {
            if (!SaveSystem.Data.wingsPurchased) return false;
            if (z - lastPowerUpZ < config.wingsMinGap) return false;
            return random.NextDouble() < config.wingsSpawnChance;
        }

        /// <summary>
        /// A calm stretch: recovery rows, two rows of coins leading into the pickup lane, the pickup
        /// itself, and one more empty row. No obstacles, so it is always reachable.
        /// </summary>
        private void PlanWings(float spacing)
        {
            int lane = random.Next(3);
            int calmRows = Mathf.Max(recoveryRows, 1);
            for (int i = 0; i < calmRows; i++)
                Enqueue(new PlannedRow { Cells = EmptyRow, SpacingAfter = spacing, Coins = true, CoinLane = coinLane });
            for (int i = 0; i < 2; i++)
                Enqueue(new PlannedRow { Cells = EmptyRow, SpacingAfter = spacing, Coins = true, CoinLane = lane });
            Enqueue(new PlannedRow { Cells = EmptyRow, SpacingAfter = spacing, PowerUp = true, CoinLane = lane });
            Enqueue(new PlannedRow { Cells = EmptyRow, SpacingAfter = spacing, Coins = false, CoinLane = lane });
            coinLane = lane;
            recoveryRows = 0;
            LastPattern = "wings";
        }

        /// <summary>
        /// Picks a pattern and validates it together with the last planned rows and the recovery rows, so
        /// patterns can follow each other closely without an impassable join. <paramref name="offset"/> is
        /// where the recovery rows start in the returned path.
        /// </summary>
        private bool TryChoosePattern(float z, float difficulty, float spacing, out PatternCell[][] rows, out PatternPath path,
            out int offset)
        {
            offset = history.Count;
            candidates.Clear();
            foreach (ObstaclePattern pattern in PatternLibrary.All)
                if (pattern.MinDifficulty <= difficulty) candidates.Add(pattern);

            for (int attempt = 0; attempt < 12 && candidates.Count > 0; attempt++)
            {
                ObstaclePattern pattern = PickWeighted(difficulty);
                rows = Instantiate(pattern, random.Next(2) == 0);
                var combined = new List<PatternCell[]>(history);
                for (int i = 0; i < recoveryRows; i++) combined.Add(EmptyRow);
                combined.AddRange(rows);
                // Check against the fastest speed the courier can have while inside this pattern.
                float end = z + (recoveryRows + rows.Length) * spacing;
                PassabilityRules rules = PassabilityRules.From(config, spacing, director.SpeedAt(end));
                if (PatternValidator.TryFindPath(combined.ToArray(), rules, -1, out path, out string error))
                {
                    recoveryRows = pattern.RecoveryRows;
                    LastPattern = pattern.Name;
                    return true;
                }
                if (Debug.isDebugBuild)
                    Debug.Log($"Pattern '{pattern.Name}' skipped at {z:0} m ({spacing:0.#} m rows): {error}");
                candidates.Remove(pattern);
            }
            rows = null;
            path = null;
            return false;
        }

        /// <summary>Patterns whose rating is close to the current difficulty are the most likely.</summary>
        private ObstaclePattern PickWeighted(float difficulty)
        {
            float total = 0f;
            foreach (ObstaclePattern pattern in candidates) total += WeightOf(pattern, difficulty);
            float roll = (float)random.NextDouble() * total;
            foreach (ObstaclePattern pattern in candidates)
            {
                roll -= WeightOf(pattern, difficulty);
                if (roll <= 0f) return pattern;
            }
            return candidates[candidates.Count - 1];
        }

        private static float WeightOf(ObstaclePattern pattern, float difficulty)
        {
            float distance = pattern.Rating - difficulty;
            return pattern.Weight * Mathf.Exp(-distance * distance / 0.08f) + 0.02f;
        }

        /// <summary>Copies a pattern, optionally mirrored, and resolves "any jumpable" cells to a wall or rock.</summary>
        private PatternCell[][] Instantiate(ObstaclePattern pattern, bool mirror)
        {
            var rows = new PatternCell[pattern.Rows.Length][];
            for (int r = 0; r < rows.Length; r++)
            {
                rows[r] = new PatternCell[3];
                for (int lane = 0; lane < 3; lane++)
                {
                    PatternCell cell = pattern.Rows[r][mirror ? 2 - lane : lane];
                    if (cell == PatternCell.Jump) cell = random.Next(3) == 0 ? PatternCell.Rock : PatternCell.Wall;
                    rows[r][lane] = cell;
                }
            }
            return rows;
        }

        private void EmitRow(SegmentBuildContext context, PlannedRow row, float localZ)
        {
            TrackSegment segment = context.Segment;
            ThemeProfile profile = segment.Profile;
            float trackZ = segment.StartZ + localZ;
            if (row.PowerUp)
            {
                if (profile.allowsPowerUps)
                {
                    context.Library.Get(PoolKeys.Wings, segment.Gameplay, segment.LanePoint(row.CoinLane - 1, localZ, 1.2f));
                    lastPowerUpZ = trackZ;
                }
                return;
            }

            bool suppress = SuppressObstacles != null && SuppressObstacles(trackZ);
            bool arcHasObstacle = false;
            for (int lane = 0; lane < 3; lane++)
            {
                string key = suppress ? null : KeyFor(row.Cells[lane], profile, context.Random);
                if (key == null) continue;
                context.Library.Get(key, segment.Gameplay, segment.LanePoint(lane - 1, localZ));
                if (lane == row.CoinLane) arcHasObstacle = true;
                // Coins in a tower's lane just before it: grabbing them means stepping into its fire.
                if (key == PoolKeys.Tower)
                    for (int i = 0; i < config.towerBaitCoins; i++)
                        context.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(lane - 1, localZ - 4.5f - i * 2.5f, 1f));
            }

            if (!row.Coins) return;
            int coinLane = row.CoinLane - 1;
            if (row.CoinArc && arcHasObstacle)
            {
                // Follows the jump over the obstacle.
                context.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(coinLane, localZ - 2.2f, 2.3f));
                context.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(coinLane, localZ, 2.85f));
                context.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(coinLane, localZ + 2.2f, 2.3f));
            }
            else
            {
                for (int i = -1; i <= 1; i++)
                    context.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(coinLane, localZ + i * 2.5f, CoinGroundHeight));
            }
        }

        /// <summary>
        /// Turns a planned cell into a pool key, respecting what the segment theme allows. Removing or
        /// softening an obstacle never breaks the path the validator found.
        /// </summary>
        private static string KeyFor(PatternCell cell, ThemeProfile profile, System.Random random)
        {
            if (cell == PatternCell.Empty || !profile.allowsObstacles) return null;
            if (cell == PatternCell.Tower && profile.allowsTowers) return PoolKeys.Tower;
            string[] variants = cell == PatternCell.Rock ? PoolKeys.Rocks : PoolKeys.Walls;
            return variants[random.Next(variants.Length)];
        }
    }
}
