using System;
using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Runs the treant event (GAME_DESIGN.md §8): a calm warning stretch, a signal, a formation of
    /// treants charging down the lanes with one lane always free, then a short recovery stretch.
    /// Treants stay in the pool until the courier enters the event.
    /// </summary>
    public sealed class TreantEncounterManager : MonoBehaviour
    {
        private sealed class Encounter
        {
            public float StartZ;
            public float EndZ;
            /// <summary>
            /// Per wave: how many treants march in a column in each lane (0 = the free lane), and how far
            /// behind the first wave it starts.
            /// </summary>
            public readonly List<(int[] columns, float offset)> Rows = new();
            public bool Launched;
        }

        /// <summary>Raised with the alert text when a formation is launched.</summary>
        public event Action<string> Warning;

        private readonly List<Encounter> encounters = new();
        private readonly List<Treant> walking = new();
        private GameWorld world;
        private GameConfig config;
        private System.Random random;

        public void Initialize(GameWorld owner, int seed)
        {
            world = owner;
            config = GameConfig.Instance;
            random = new System.Random(seed ^ 0x5eed);
            world.Segments.TreantEventScheduled += Schedule;
            world.Segments.SegmentPlaced += OnSegmentPlaced;
        }

        private void Schedule(float startZ, float endZ)
        {
            var encounter = new Encounter { StartZ = startZ, EndZ = endZ };
            float difficulty = world.Difficulty.DifficultyAt(startZ);
            // Several waves; the free lane moves between them, so the courier has to keep switching.
            int waves = difficulty < 0.3f ? 2 : difficulty < 0.6f ? 2 + random.Next(2) : 3;
            int maxColumn = Mathf.Clamp(difficulty < 0.3f ? 2 : config.treantColumnMax, 1, 6);
            int free = random.Next(3);
            float offset = 0f;
            for (int wave = 0; wave < waves; wave++)
            {
                var columns = new int[3];
                int longest = 0;
                for (int lane = 0; lane < 3; lane++)
                {
                    if (lane == free) continue;
                    columns[lane] = 1 + random.Next(maxColumn);
                    longest = Mathf.Max(longest, columns[lane]);
                }
                encounter.Rows.Add((columns, offset));
                offset += (longest - 1) * config.treantColumnSpacing + config.treantRowGap;
                free = (free + 1 + random.Next(2)) % 3;
            }
            encounters.Add(encounter);
        }

        /// <summary>Lays a coin trail along the free lane so the safe line is readable from afar.</summary>
        private void OnSegmentPlaced(TrackSegment segment)
        {
            if (segment.Theme != SegmentTheme.TreantGrove) return;
            Encounter encounter = encounters.Find(e => segment.StartZ >= e.StartZ && segment.StartZ < e.EndZ);
            if (encounter == null) return;

            float speed = world.Difficulty.SpeedAt(encounter.StartZ);
            for (int row = 0; row < encounter.Rows.Count; row++)
            {
                int free = Array.IndexOf(encounter.Rows[row].columns, 0);
                float from = row == 0 ? encounter.StartZ + 18f : LastMeetingZ(encounter, row - 1, speed) + 4f;
                float to = MeetingZ(encounter, row, speed) - 3f;
                for (float z = from; z <= to; z += 3f)
                {
                    if (z < segment.StartZ || z >= segment.EndZ) continue;
                    world.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(free - 1, z - segment.StartZ, 1f));
                }
            }
        }

        /// <summary>Track distance where the courier and the given treant row pass each other.</summary>
        private float MeetingZ(Encounter encounter, int row, float speed)
        {
            float launchZ = encounter.StartZ + config.treantTriggerOffset;
            float spawnZ = SpawnZ(encounter, row);
            return launchZ + (spawnZ - launchZ) * speed / (speed + config.treantSpeed);
        }

        /// <summary>Where the courier passes the last treant of a wave's longest column.</summary>
        private float LastMeetingZ(Encounter encounter, int row, float speed)
        {
            int longest = Mathf.Max(encounter.Rows[row].columns[0], Mathf.Max(encounter.Rows[row].columns[1], encounter.Rows[row].columns[2]));
            float columnLength = (longest - 1) * config.treantColumnSpacing;
            return MeetingZ(encounter, row, speed) + columnLength * speed / (speed + config.treantSpeed);
        }

        private float SpawnZ(Encounter encounter, int row) => encounter.EndZ - 4f + encounter.Rows[row].offset;

        private void Update()
        {
            if (world == null) return;
            float runnerZ = world.RunnerTrackZ;
            foreach (Encounter encounter in encounters)
            {
                if (encounter.Launched || runnerZ < encounter.StartZ + config.treantTriggerOffset) continue;
                Launch(encounter);
            }
            encounters.RemoveAll(e => e.Launched && runnerZ > e.EndZ + 40f);
            UpdateShake(runnerZ);
        }

        private void Launch(Encounter encounter)
        {
            encounter.Launched = true;
            for (int row = 0; row < encounter.Rows.Count; row++)
            {
                (int[] columns, float _) = encounter.Rows[row];
                for (int lane = 0; lane < 3; lane++)
                    for (int i = 0; i < columns[lane]; i++)
                    {
                        // A column marches single file down the lane.
                        Vector3 local = new((lane - 1) * config.laneWidth, 0f, SpawnZ(encounter, row) + i * config.treantColumnSpacing);
                        // Treant templates face -Z, toward the oncoming courier.
                        Treant treant = world.Library.Get(PoolKeys.Treant, world.Root, local).GetComponent<Treant>();
                        treant.Launch(config.treantSpeed);
                        walking.Add(treant);
                    }
            }
            Warning?.Invoke("ТРЕНТЫ ИДУТ!");
            if (world.Runner.IsRunning) world.RunnerCamera.AddShake(0.35f);
        }

        /// <summary>The ground trembles more the closer the nearest walking treant is.</summary>
        private void UpdateShake(float runnerZ)
        {
            walking.RemoveAll(treant => treant == null || !treant.isActiveAndEnabled);
            if (walking.Count == 0 || !world.Runner.IsRunning) return;
            float courierZ = world.Runner.transform.position.z;
            float nearest = float.PositiveInfinity;
            foreach (Treant treant in walking)
                nearest = Mathf.Min(nearest, Mathf.Abs(treant.transform.position.z - courierZ));
            float shake = Mathf.Clamp01(1f - nearest / 90f) * 0.18f;
            if (shake > 0f) world.RunnerCamera.HoldShake(shake);
        }
    }
}
