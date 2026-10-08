namespace CourierRunner
{
    /// <summary>Everything a populator needs to fill one segment with coins and obstacles.</summary>
    public sealed class SegmentBuildContext
    {
        public TrackSegment Segment;
        public ModelLibrary Library;
        /// <summary>Random source for this segment only (deterministic per seed and segment index).</summary>
        public System.Random Random;
        /// <summary>No gameplay rows are generated at or beyond this track distance.</summary>
        public float StopZ = float.PositiveInfinity;
    }

    /// <summary>Fills freshly placed segments with gameplay objects. Segments arrive in track order.</summary>
    public interface ISegmentPopulator
    {
        void BeginRun(RunMode mode, int seed);
        void Populate(SegmentBuildContext context);
    }
}
