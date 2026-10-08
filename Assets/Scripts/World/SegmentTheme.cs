namespace CourierRunner
{
    /// <summary>Visual and gameplay flavour of one track segment (see GAME_DESIGN.md §4).</summary>
    public enum SegmentTheme
    {
        DenseForest,
        SparseForest,
        Rocky,
        Boulders,
        Towers,
        Clearing,
        TreantGrove,
        Safe,
        Finish,
    }

    /// <summary>Decoration density and which gameplay elements a theme accepts.</summary>
    public sealed class ThemeProfile
    {
        /// <summary>Trees close to the road (x 6.8–11 m).</summary>
        public int nearTreesPerSide = 4;
        /// <summary>Trees further out (x 11–20 m) that fill the gaps.</summary>
        public int farTreesPerSide = 5;
        public float nearTreeMinX = 6.8f;
        public float pineShare = 0.72f;
        public int rocksPerSide = 2;
        public int stumps;
        public int wells;
        /// <summary>Painted tree groups (PointForest cards) at x 13–30 m.</summary>
        public int forestCardsPerSide = 4;
        /// <summary>Wide painted forest walls behind everything (ForestSide images).</summary>
        public int forestWallsPerSide = 1;

        public bool allowsObstacles = true;
        public bool allowsTowers = true;
        public bool allowsPowerUps = true;
        /// <summary>False for event segments: the pattern generator leaves them to the event.</summary>
        public bool allowsGeneratedRows = true;

        public static ThemeProfile For(SegmentTheme theme) => theme switch
        {
            SegmentTheme.DenseForest => new ThemeProfile { nearTreesPerSide = 7, farTreesPerSide = 9, pineShare = 0.78f, rocksPerSide = 2, stumps = 1, forestCardsPerSide = 7 },
            SegmentTheme.SparseForest => new ThemeProfile { nearTreesPerSide = 4, farTreesPerSide = 8, nearTreeMinX = 7.5f, rocksPerSide = 3, stumps = 2, forestCardsPerSide = 6 },
            SegmentTheme.Rocky => new ThemeProfile { nearTreesPerSide = 4, farTreesPerSide = 7, pineShare = 0.75f, rocksPerSide = 6, forestCardsPerSide = 6 },
            SegmentTheme.Boulders => new ThemeProfile { nearTreesPerSide = 3, farTreesPerSide = 7, pineShare = 0.7f, rocksPerSide = 9, forestCardsPerSide = 6 },
            SegmentTheme.Towers => new ThemeProfile { nearTreesPerSide = 5, farTreesPerSide = 8, rocksPerSide = 3, forestCardsPerSide = 6 },
            SegmentTheme.Clearing => new ThemeProfile { nearTreesPerSide = 2, farTreesPerSide = 7, nearTreeMinX = 9f, rocksPerSide = 3, stumps = 3, wells = 1, forestCardsPerSide = 6 },
            SegmentTheme.TreantGrove => new ThemeProfile { nearTreesPerSide = 3, farTreesPerSide = 7, nearTreeMinX = 8.5f, pineShare = 0.55f, rocksPerSide = 1, forestCardsPerSide = 5, allowsObstacles = false, allowsTowers = false, allowsPowerUps = false, allowsGeneratedRows = false },
            SegmentTheme.Safe => new ThemeProfile { nearTreesPerSide = 6, farTreesPerSide = 8, rocksPerSide = 2, forestCardsPerSide = 6, allowsObstacles = false, allowsTowers = false },
            SegmentTheme.Finish => new ThemeProfile { nearTreesPerSide = 2, farTreesPerSide = 6, nearTreeMinX = 9f, stumps = 1, forestCardsPerSide = 5, allowsObstacles = false, allowsTowers = false, allowsPowerUps = false },
            _ => new ThemeProfile(),
        };
    }
}
