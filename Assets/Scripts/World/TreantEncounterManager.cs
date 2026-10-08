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
            /// <summary>Per row: which lanes (0..2) are blocked, and how far behind the first row it walks.</summary>
            public readonly List<(bool[] blocked, float offset)> Rows = new();
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
            double roll = random.NextDouble();
            if (difficulty < 0.25f && roll < 0.4)
            {
                // One central treant: the gentlest introduction.
                encounter.Rows.Add((new[] { false, true, false }, 0f));
            }
            else if (difficulty >= 0.35f && roll < 0.35 + difficulty * 0.4)
            {
                // Two rows: the free lane moves, so the courier must switch lanes between them.
                int firstFree = random.Next(3);
                int secondFree = (firstFree + 1 + random.Next(2)) % 3;
                encounter.Rows.Add((Blocked(firstFree), 0f));
                encounter.Rows.Add((Blocked(secondFree), config.treantRowGap));
            }
            else
            {
                encounter.Rows.Add((Blocked(random.Next(3)), 0f));
            }
            encounters.Add(encounter);
        }

        private static bool[] Blocked(int freeLane)
        {
            var blocked = new bool[3];
            for (int lane = 0; lane < 3; lane++) blocked[lane] = lane != freeLane;
            return blocked;
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
                int free = Array.IndexOf(encounter.Rows[row].blocked, false);
                float from = row == 0 ? encounter.StartZ + 18f : MeetingZ(encounter, row - 1, speed) + 4f;
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
                (bool[] blocked, float _) = encounter.Rows[row];
                for (int lane = 0; lane < 3; lane++)
                {
                    if (!blocked[lane]) continue;
                    Vector3 local = new((lane - 1) * config.laneWidth, 0f, SpawnZ(encounter, row));
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
