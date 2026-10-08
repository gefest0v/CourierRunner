using System;
using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Keeps a fixed number of segments around the courier: the one that falls behind the camera
    /// is cleared, re-themed, refilled and moved to the front of the road.
    /// </summary>
    public sealed class SegmentManager : MonoBehaviour
    {
        public event Action<TrackSegment> SegmentPlaced;
        /// <summary>A treant event will occupy [start, end) of the track. Raised before its segments are placed.</summary>
        public event Action<float, float> TreantEventScheduled;

        private readonly List<TrackSegment> active = new();
        private readonly Stack<TrackSegment> spare = new();
        private readonly SegmentBuildContext context = new();
        private readonly Queue<SegmentTheme> forcedThemes = new();

        private GameWorld world;
        private GameConfig config;
        private ISegmentPopulator populator;
        private System.Random themeRandom;
        private int seed;
        private int nextIndex;
        private float nextStartZ;
        private bool finishPlaced;
        private SegmentTheme lastTheme;
        private int themeRepeat;
        private float lastEventEndZ;

        public IReadOnlyList<TrackSegment> Active => active;
        public RunMode Mode { get; private set; }
        /// <summary>Track Z of the delivery point in story mode, or infinity in endless mode.</summary>
        public float FinishZ { get; private set; } = float.PositiveInfinity;

        public void Initialize(GameWorld owner, ISegmentPopulator segmentPopulator)
        {
            world = owner;
            config = GameConfig.Instance;
            populator = segmentPopulator;
            int poolSize = config.segmentsAhead + config.segmentsBehind + 2;
            for (int i = 0; i < poolSize; i++)
                spare.Push(TrackSegment.Create(world.Root, config.segmentLength, world.GrassMaterial, world.PathMaterial));
        }

        public void SetPopulator(ISegmentPopulator segmentPopulator) => populator = segmentPopulator;

        public void Begin(RunMode mode, int runSeed)
        {
            foreach (TrackSegment segment in active)
            {
                segment.Clear();
                spare.Push(segment);
            }
            active.Clear();

            Mode = mode;
            seed = runSeed;
            themeRandom = new System.Random(runSeed);
            finishPlaced = false;
            forcedThemes.Clear();
            lastEventEndZ = float.NegativeInfinity;
            FinishZ = mode == RunMode.Story ? config.storyLevelLength : float.PositiveInfinity;
            context.StopZ = mode == RunMode.Story ? config.storyLevelLength - config.storyCalmDistance : float.PositiveInfinity;
            populator?.BeginRun(mode, runSeed);
            nextIndex = -config.segmentsBehind;
            nextStartZ = nextIndex * config.segmentLength;
            FillAhead(0f);
            UpdateZones(0f);
        }

        private void Update()
        {
            if (world == null || world.Runner == null) return;
            float runnerZ = world.RunnerTrackZ;
            RecycleBehind(runnerZ);
            FillAhead(runnerZ);
            UpdateZones(runnerZ);
        }

        private void RecycleBehind(float runnerZ)
        {
            while (active.Count > 0 && active[0].EndZ < runnerZ - config.recycleBehindDistance)
            {
                TrackSegment segment = active[0];
                active.RemoveAt(0);
                segment.Clear();
                spare.Push(segment);
            }
        }

        private void FillAhead(float runnerZ)
        {
            float horizon = runnerZ + config.segmentsAhead * config.segmentLength;
            while (nextStartZ < horizon && spare.Count > 0)
                PlaceNext();
        }

        private void PlaceNext()
        {
            TrackSegment segment = spare.Pop();
            SegmentTheme theme = ChooseTheme(nextStartZ);
            segment.Place(nextIndex, nextStartZ, theme);
            var random = new System.Random(unchecked(seed * 486187739 + nextIndex * 7919));
            SegmentDecorator.Decorate(segment, world.Library, random);

            if (theme == SegmentTheme.Finish)
            {
                PlaceFinish(segment);
            }
            else if (nextStartZ >= 0f && nextStartZ < FinishZ)
            {
                context.Segment = segment;
                context.Library = world.Library;
                context.Random = random;
                populator?.Populate(context);
            }

            active.Add(segment);
            nextIndex++;
            nextStartZ += config.segmentLength;
            SegmentPlaced?.Invoke(segment);
        }

        private SegmentTheme ChooseTheme(float startZ)
        {
            if (startZ < config.safeStartDistance) return SegmentTheme.Safe;
            if (Mode == RunMode.Story)
            {
                if (!finishPlaced && startZ >= FinishZ)
                {
                    finishPlaced = true;
                    return SegmentTheme.Finish;
                }
                if (finishPlaced) return SegmentTheme.Clearing;
                // The last segment before the delivery point is calm so the recipient is easy to see.
                if (startZ + config.segmentLength >= FinishZ) return SegmentTheme.Safe;
            }

            if (forcedThemes.Count > 0) return forcedThemes.Dequeue();
            if (ShouldStartTreantEvent(startZ))
            {
                // Warning stretch + charge + recovery: all grove segments are free of other obstacles.
                int groveSegments = Mathf.Max(2, config.treantEventSegments);
                float endZ = startZ + groveSegments * config.segmentLength;
                for (int i = 1; i < groveSegments; i++) forcedThemes.Enqueue(SegmentTheme.TreantGrove);
                forcedThemes.Enqueue(SegmentTheme.Safe);
                lastEventEndZ = endZ;
                TreantEventScheduled?.Invoke(startZ, endZ);
                return SegmentTheme.TreantGrove;
            }

            SegmentTheme theme = RegularThemes[themeRandom.Next(RegularThemes.Length)];
            // Never show the same look more than twice in a row.
            for (int attempt = 0; theme == lastTheme && themeRepeat >= 1 && attempt < 8; attempt++)
                theme = RegularThemes[themeRandom.Next(RegularThemes.Length)];
            themeRepeat = theme == lastTheme ? themeRepeat + 1 : 0;
            lastTheme = theme;
            return theme;
        }

        private bool ShouldStartTreantEvent(float startZ)
        {
            if (startZ < config.treantMinDistance) return false;
            float difficulty = world.Difficulty.DifficultyAt(startZ);
            float cooldown = Mathf.Lerp(config.treantCooldownEasy, config.treantCooldownHard, difficulty);
            if (startZ - lastEventEndZ < cooldown) return false;
            // The event plus its recovery segment must end well before the story recipient.
            float eventEnd = startZ + (config.treantEventSegments + 1) * config.segmentLength;
            float limit = config.storyLevelLength - config.storyCalmDistance;
            if (Mode == RunMode.Story && eventEnd > limit) return false;
            // A story level always gets its treants: the last possible slot is taken for sure.
            bool lastChance = Mode == RunMode.Story && eventEnd + config.segmentLength > limit;
            return lastChance || themeRandom.NextDouble() < config.treantChance;
        }

        private static readonly SegmentTheme[] RegularThemes =
        {
            SegmentTheme.DenseForest, SegmentTheme.DenseForest, SegmentTheme.SparseForest, SegmentTheme.Rocky,
            SegmentTheme.Fenced, SegmentTheme.Towers, SegmentTheme.Clearing,
        };

        private void PlaceFinish(TrackSegment segment)
        {
            // The finish line sits a little into the segment, with the recipient a few meters behind it.
            const float finishOffset = 12f;
            FinishZ = segment.StartZ + finishOffset;
            world.Library.CreateCrystalMaiden(segment.Decor, new Vector3(0f, 0f, finishOffset + 5f));

            var finish = new GameObject("Delivery Finish");
            finish.transform.SetParent(segment.Gameplay, false);
            finish.transform.localPosition = new Vector3(0f, 1f, finishOffset);
            BoxCollider trigger = finish.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(11f, 4f, 4f);
            finish.AddComponent<FinishTrigger>().runner = world.Runner;
        }

        private void UpdateZones(float runnerZ)
        {
            foreach (TrackSegment segment in active)
            {
                float ahead = segment.StartZ - runnerZ;
                TrackZone zone = ahead <= config.nearZoneDistance ? TrackZone.Near :
                    ahead <= config.midZoneDistance ? TrackZone.Mid : TrackZone.Far;
                segment.SetZone(zone, segment.Zone == TrackZone.Hidden);
            }
        }
    }
}
