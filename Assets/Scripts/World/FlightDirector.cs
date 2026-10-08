using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Reacts to the courier taking off: lays a separate line of air coins that switches lanes
    /// (so flying still needs steering) and clears obstacles where the courier will land.
    /// </summary>
    public sealed class FlightDirector : MonoBehaviour
    {
        private const float ClimbDistance = 14f;
        private const float LandingBefore = 14f;
        private const float LandingAfter = 18f;

        private readonly List<Transform> scratch = new();
        private GameWorld world;
        private GameConfig config;
        private System.Random random;
        /// <summary>Air coins are only laid beyond this track distance, so extending a flight never doubles them.</summary>
        private float airCoinsUntil;
        private float landingFrom = float.NegativeInfinity;
        private float landingTo = float.NegativeInfinity;

        /// <summary>True inside the current landing window; the generator places no obstacles there.</summary>
        public bool IsLandingZone(float trackZ) => trackZ >= landingFrom && trackZ <= landingTo;

        public void Initialize(GameWorld owner, int seed)
        {
            world = owner;
            config = GameConfig.Instance;
            random = new System.Random(seed ^ 0x7a1);
            world.Runner.FlightStarted += OnFlightStarted;
        }

        private void OnFlightStarted(float remaining)
        {
            RunnerController runner = world.Runner;
            float startZ = world.RunnerTrackZ;
            // Speed only rises during a run, so this slightly underestimates where the landing is.
            float landingZ = startZ + world.Difficulty.SpeedAt(startZ) * (remaining + 0.6f);
            LayAirCoins(Mathf.Max(startZ + ClimbDistance, airCoinsUntil), landingZ - LandingBefore, runner.Lane + 1);
            airCoinsUntil = landingZ - LandingBefore;
            landingFrom = landingZ - LandingBefore;
            landingTo = landingZ + LandingAfter;
            ClearLanding(landingFrom, landingTo);
        }

        private void LayAirCoins(float from, float to, int lane)
        {
            float height = config.flightHeight + 1f;
            float nextSwitch = from + config.airCoinLaneSwitch * 0.5f;
            for (float z = from; z < to; z += 3f)
            {
                if (z >= nextSwitch)
                {
                    lane = (lane + 1 + random.Next(2)) % 3;
                    nextSwitch = z + config.airCoinLaneSwitch;
                    z += 3f; // A short gap marks the switch.
                    if (z >= to) break;
                }
                TrackSegment segment = world.Segments.SegmentAt(z);
                if (segment == null) continue;
                world.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(lane - 1, z - segment.StartZ, height));
            }
        }

        /// <summary>Removes obstacles in the landing window so a descent never ends in a wall.</summary>
        private void ClearLanding(float from, float to)
        {
            foreach (TrackSegment segment in world.Segments.Active)
            {
                if (segment.EndZ < from || segment.StartZ > to) continue;
                scratch.Clear();
                foreach (Transform child in segment.Gameplay) scratch.Add(child);
                foreach (Transform child in scratch)
                {
                    float z = segment.StartZ + child.localPosition.z;
                    if (z < from || z > to) continue;
                    if (child.GetComponentInChildren<ObstacleTrigger>(true) != null)
                        PooledObject.ReleaseOrDestroy(child.gameObject);
                }
            }
        }
    }
}
