namespace CourierRunner
{
    /// <summary>Visual and gameplay flavour of one track segment (see GAME_DESIGN.md §4).</summary>
    public enum SegmentTheme
    {
        DenseForest,
        SparseForest,
        Rocky,
        Fenced,
        Towers,
        Clearing,
        TreantGrove,
        Safe,
        Finish,
    }

    /// <summary>Decoration density and which gameplay elements a theme accepts.</summary>
    public sealed class ThemeProfile
    {
        public int treesPerSide;
        public float treeMinX = 7f;
        public float treeMaxX = 18f;
        public float pineShare = 0.5f;
        public int rocksPerSide;
        public int stumps;
        public bool fences;
        public int wells;
        public int forestCardsPerSide = 2;

        public bool allowsObstacles = true;
        public bool allowsTowers;
        public bool allowsPowerUps = true;

        public static ThemeProfile For(SegmentTheme theme) => theme switch
        {
            SegmentTheme.DenseForest => new ThemeProfile { treesPerSide = 9, treeMinX = 6.8f, treeMaxX = 16f, pineShare = 0.6f, rocksPerSide = 1, stumps = 1 },
            SegmentTheme.SparseForest => new ThemeProfile { treesPerSide = 4, treeMinX = 8f, treeMaxX = 19f, rocksPerSide = 1, stumps = 1 },
            SegmentTheme.Rocky => new ThemeProfile { treesPerSide = 4, treeMinX = 9f, rocksPerSide = 4, stumps = 0, pineShare = 0.7f },
            SegmentTheme.Fenced => new ThemeProfile { treesPerSide = 5, treeMinX = 8.5f, fences = true, rocksPerSide = 1 },
            SegmentTheme.Towers => new ThemeProfile { treesPerSide = 5, treeMinX = 8f, rocksPerSide = 2, allowsTowers = true },
            SegmentTheme.Clearing => new ThemeProfile { treesPerSide = 2, treeMinX = 11f, treeMaxX = 19f, stumps = 2, wells = 1 },
            SegmentTheme.TreantGrove => new ThemeProfile { treesPerSide = 6, treeMinX = 9f, treeMaxX = 18f, pineShare = 0.2f, allowsObstacles = false, allowsPowerUps = false },
            SegmentTheme.Safe => new ThemeProfile { treesPerSide = 6, treeMinX = 7.5f, rocksPerSide = 1, allowsObstacles = false },
            SegmentTheme.Finish => new ThemeProfile { treesPerSide = 3, treeMinX = 10f, stumps = 1, allowsObstacles = false, allowsPowerUps = false },
            _ => new ThemeProfile { treesPerSide = 5 },
        };
    }
}
