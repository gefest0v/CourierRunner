using UnityEngine;

namespace CourierRunner
{
    /// <summary>Places the theme's trees, rocks and props along both sides of a segment.</summary>
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
                PlaceStratified(profile.treesPerSide, length, random, z =>
                {
                    string key = random.NextDouble() < profile.pineShare ? PoolKeys.TreePine : PoolKeys.TreeOak;
                    float x = side * Range(random, profile.treeMinX, profile.treeMaxX);
                    GameObject tree = library.Get(key, segment.Decor, new Vector3(x, 0f, z), Range(random, 0f, 360f));
                    tree.transform.localScale = Vector3.one * Range(random, 0.82f, 1.18f);
                });

                PlaceStratified(profile.rocksPerSide, length, random, z =>
                {
                    float x = side * Range(random, RoadClearance + 0.8f, 14f);
                    GameObject rock = library.Get(PoolKeys.DecorRock, segment.Decor, new Vector3(x, 0f, z), Range(random, 0f, 360f));
                    rock.transform.localScale = Vector3.one * Range(random, 0.65f, 1.4f);
                });

                PlaceStratified(profile.forestCardsPerSide, length, random, z =>
                {
                    int variant = random.Next(ModelLibrary.ForestCardVariants);
                    float x = side * Range(random, 22f, 27f);
                    GameObject card = library.Get(ModelLibrary.ForestCardKey(variant), segment.Decor, new Vector3(x, 0f, z));
                    card.transform.localScale = Vector3.one * Range(random, 0.9f, 1.12f);
                });

                if (profile.fences)
                {
                    // A run of low fence pieces lining the road edge.
                    const float pieceLength = 3.2f;
                    for (float z = 2f; z < length - 2f; z += pieceLength + Range(random, 0.2f, 1.4f))
                    {
                        GameObject fence = library.Get(PoolKeys.Fence, segment.Decor,
                            new Vector3(side * (RoadClearance + 0.4f), 0f, z), 90f);
                        fence.transform.localScale = new Vector3(1f, Range(random, 0.85f, 1.1f), 1f);
                    }
                }
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
