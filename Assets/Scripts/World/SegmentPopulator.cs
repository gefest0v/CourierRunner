using UnityEngine;

namespace CourierRunner
{
    /// <summary>Everything a populator needs to fill one segment with coins and obstacles.</summary>
    public sealed class SegmentBuildContext
    {
        public TrackSegment Segment;
        public ModelLibrary Library;
        public System.Random Random;
        public RunMode Mode;
        /// <summary>Distance of the segment start from the run start, in meters.</summary>
        public float Distance;
    }

    /// <summary>Fills a freshly placed segment with gameplay objects.</summary>
    public interface ISegmentPopulator
    {
        void Populate(SegmentBuildContext context);
    }

    /// <summary>
    /// Stage 1 stand-in: one jumpable obstacle per row with a coin line in a free lane.
    /// Replaced by the pattern generator in stage 2.
    /// </summary>
    public sealed class SimpleSegmentPopulator : ISegmentPopulator
    {
        private const float RowSpacing = 10f;

        public void Populate(SegmentBuildContext context)
        {
            TrackSegment segment = context.Segment;
            System.Random random = context.Random;
            for (float z = RowSpacing * 0.5f; z < segment.Length; z += RowSpacing)
            {
                int obstacleLane = 99;
                if (segment.Profile.allowsObstacles && random.NextDouble() < 0.55)
                {
                    obstacleLane = random.Next(3) - 1;
                    bool tower = segment.Profile.allowsTowers && random.NextDouble() < 0.5;
                    string key = tower ? PoolKeys.Tower : random.Next(2) == 0 ? PoolKeys.Wall : PoolKeys.Rock;
                    context.Library.Get(key, segment.Gameplay, segment.LanePoint(obstacleLane, z));
                }

                int coinLane = random.Next(3) - 1;
                if (coinLane == obstacleLane) coinLane = coinLane == 1 ? 0 : coinLane + 1;
                for (int i = 0; i < 3; i++)
                    context.Library.Get(PoolKeys.Coin, segment.Gameplay, segment.LanePoint(coinLane, z - 3f + i * 2.5f, 1f));
            }
        }
    }
}
