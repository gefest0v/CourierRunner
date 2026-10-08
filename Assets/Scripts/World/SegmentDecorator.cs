using UnityEngine;
using UnityEngine.Rendering;

namespace CourierRunner
{
    /// <summary>
    /// Fills both sides of a segment in depth layers: trees by the road, more trees behind them,
    /// painted tree groups further out and a wide painted forest wall at the back, plus rocks and props.
    /// </summary>
    public static class SegmentDecorator
    {
        /// <summary>Half-width of the road plus a margin; decoration never comes closer to the center.</summary>
        private const float RoadClearance = 5.9f;

        public static void Decorate(TrackSegment segment, ModelLibrary library, System.Random random)
        {
            ThemeProfile profile = segment.Profile;
            float length = segment.Length;

            for (int side = -1; side <= 1; side += 2)
            {
                int s = side;
                PlaceStratified(profile.nearTreesPerSide, length, random, z =>
                    PlaceTree(segment, library, random, profile, s * Range(random, profile.nearTreeMinX, 11f), z));
                PlaceStratified(profile.farTreesPerSide, length, random, z =>
                    PlaceTree(segment, library, random, profile, s * Range(random, 11f, 20f), z));

                PlaceStratified(profile.rocksPerSide, length, random, z =>
                {
                    float x = s * Range(random, RoadClearance + 0.6f, 15f);
                    string key = PoolKeys.DecorRocks[random.Next(PoolKeys.DecorRocks.Length)];
                    GameObject rock = library.Get(key, segment.Decor, new Vector3(x, 0f, z), Range(random, 0f, 360f));
                    rock.transform.localScale = Vector3.one * Range(random, 0.6f, 1.6f);
                });

                PlaceStratified(profile.forestCardsPerSide, length, random, z =>
                {
                    int variant = random.Next(ModelLibrary.ForestCardVariants);
                    float x = s * Range(random, 10f, 30f);
                    GameObject card = library.Get(ModelLibrary.ForestCardKey(variant), segment.Decor, new Vector3(x, 0f, z));
                    // Further cards are larger so the forest keeps getting taller toward the back.
                    float depth = Mathf.InverseLerp(10f, 30f, Mathf.Abs(x));
                    card.transform.localScale = Vector3.one * Range(random, 0.9f, 1.25f) * Mathf.Lerp(1f, 1.5f, depth);
                });

                PlaceStratified(profile.forestWallsPerSide, length, random, z =>
                {
                    int variant = random.Next(ModelLibrary.ForestWallVariants);
                    float x = s * Range(random, 23f, 27f);
                    GameObject wall = library.Get(ModelLibrary.ForestWallKey(variant), segment.Decor, new Vector3(x, 0f, z));
                    wall.transform.localScale = Vector3.one * Range(random, 0.9f, 1.15f);
                });
            }

            for (int i = 0; i < profile.stumps; i++)
            {
                float x = Side(random) * Range(random, RoadClearance + 0.7f, 12f);
                library.Get(PoolKeys.TreeStump, segment.Decor, new Vector3(x, 0f, Range(random, 2f, length - 2f)), Range(random, 0f, 360f));
            }

            for (int i = 0; i < profile.wells; i++)
            {
                float x = Side(random) * Range(random, 8.5f, 10f);
                library.Get(PoolKeys.Well, segment.Decor, new Vector3(x, 0f, Range(random, 8f, length - 8f)), Range(random, 0f, 360f));
            }
        }

        private static void PlaceTree(TrackSegment segment, ModelLibrary library, System.Random random, ThemeProfile profile, float x, float z)
        {
            string key = random.NextDouble() < profile.pineShare ? PoolKeys.TreePine : PoolKeys.TreeOak;
            GameObject tree = library.Get(key, segment.Decor, new Vector3(x, 0f, z), Range(random, 0f, 360f));
            // Trees further from the road grow a little taller.
            float depth = Mathf.InverseLerp(7f, 20f, Mathf.Abs(x));
            tree.transform.localScale = Vector3.one * Range(random, 0.85f, 1.2f) * Mathf.Lerp(1f, 1.35f, depth);
            // Only trees near the road cast shadows; the rest would cost a lot in the browser for little gain.
            ShadowCastingMode shadows = Mathf.Abs(x) < 11f ? ShadowCastingMode.On : ShadowCastingMode.Off;
            tree.GetComponentsInChildren(Renderers);
            foreach (Renderer renderer in Renderers) renderer.shadowCastingMode = shadows;
        }

        private static readonly System.Collections.Generic.List<Renderer> Renderers = new();

        /// <summary>Spreads <paramref name="count"/> items evenly along the segment with some jitter.</summary>
        private static void PlaceStratified(int count, float length, System.Random random, System.Action<float> place)
        {
            if (count <= 0) return;
            float step = length / count;
            for (int i = 0; i < count; i++)
                place(step * i + Range(random, 0.1f, 0.9f) * step);
        }

        public static float Range(System.Random random, float min, float max) =>
            min + (float)random.NextDouble() * (max - min);

        private static float Side(System.Random random) => random.Next(2) == 0 ? -1f : 1f;
    }
}
