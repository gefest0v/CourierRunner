using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// All gameplay tuning in one asset. Edit Assets/Resources/GameConfig.asset in the Inspector;
    /// if the asset is missing the defaults below are used.
    /// </summary>
    [CreateAssetMenu(menuName = "Courier Runner/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Runner")]
        public float startSpeed = 11f;
        public float laneWidth = 3f;
        public float laneChangeSpeed = 12f;
        public float jumpHeight = 2.15f;
        [Tooltip("Ground distance covered by one jump, in meters. Constant at every speed so obstacle patterns stay passable.")]
        public float jumpLength = 9f;
        [Tooltip("Minimum swipe length in pixels before it counts as a lane change or jump.")]
        public float swipeThreshold = 40f;

        [Header("Difficulty")]
        [Tooltip("Run speed at full difficulty.")]
        public float maxSpeed = 21f;
        [Tooltip("Distance between obstacle rows at difficulty 0.")]
        public float rowSpacingEasy = 13f;
        [Tooltip("Distance between obstacle rows at difficulty 1.")]
        public float rowSpacingHard = 9f;
        [Range(0f, 1f)] public float storyDifficultyStart = 0.2f;
        [Range(0f, 1f)] public float storyDifficultyEnd = 0.65f;
        [Tooltip("Endless mode reaches ~63 % difficulty after this many meters.")]
        public float endlessDifficultyDistance = 1400f;
        [Tooltip("Difficulty rises in this many discrete steps.")]
        public int difficultySteps = 10;
        public float towerFireIntervalEasy = 1.6f;
        public float towerFireIntervalHard = 0.8f;

        [Header("Track")]
        [Tooltip("Length of one road segment in meters. Keep it a multiple of 20 so ground textures tile seamlessly.")]
        public float segmentLength = 40f;
        [Tooltip("How many segments exist ahead of the courier.")]
        public int segmentsAhead = 5;
        [Tooltip("How many segments stay behind the courier (the camera looks slightly back).")]
        public int segmentsBehind = 1;
        [Tooltip("A segment is recycled once its end is this far behind the courier.")]
        public float recycleBehindDistance = 15f;
        [Tooltip("Segments starting closer than this are fully active (colliders, towers).")]
        public float nearZoneDistance = 30f;
        [Tooltip("Segments starting closer than this show gameplay objects without colliders; further ones show only scenery.")]
        public float midZoneDistance = 90f;
        [Tooltip("Obstacle-free distance at the start of every run.")]
        public float safeStartDistance = 80f;
        [Tooltip("The world is shifted back to the origin when the courier passes this Z.")]
        public float originShiftDistance = 400f;

        [Header("Story level")]
        [Tooltip("Distance to Crystal Maiden in meters.")]
        public float storyLevelLength = 700f;
        [Tooltip("Story levels are generated from a fixed seed so every attempt is the same road.")]
        public int storySeed = 1001;
        [Tooltip("Obstacle-free run-up before the recipient.")]
        public float storyCalmDistance = 25f;

        [Header("Treants")]
        [Tooltip("No treant event before this distance.")]
        public float treantMinDistance = 160f;
        [Tooltip("Minimum distance between treant events at difficulty 0.")]
        public float treantCooldownEasy = 300f;
        [Tooltip("Minimum distance between treant events at difficulty 1.")]
        public float treantCooldownHard = 140f;
        [Range(0f, 1f)] [Tooltip("Chance per eligible segment that an event starts.")]
        public float treantChance = 0.75f;
        public float treantSpeed = 8f;
        [Tooltip("Obstacle-free grove segments that make up one event (warning, charge, pass).")]
        public int treantEventSegments = 4;
        [Tooltip("The treants start walking when the courier is this far into the event.")]
        public float treantTriggerOffset = 10f;
        [Tooltip("Distance between the two rows of a double formation.")]
        public float treantRowGap = 30f;
        [Tooltip("Most treants marching one behind another in a single lane.")]
        public int treantColumnMax = 3;
        [Tooltip("Distance between treants in one column.")]
        public float treantColumnSpacing = 3.6f;
        [Tooltip("Coins laid in a tower's lane just before it, tempting the courier into its line of fire.")]
        public int towerBaitCoins = 3;

        [Header("Backdrop")]
        [Tooltip("Distance from the camera to the painted forest horizon.")]
        public float backdropDistance = 80f;

        [Header("Economy")]
        [Tooltip("Keep the coins collected in a failed run. The design doc recommends true for the prototype.")]
        public bool keepCoinsOnFail = true;
        public int wingsPrice = 300;

        [Header("Wings")]
        [Tooltip("Flight time in seconds before upgrades.")]
        public float flightDuration = 10f;
        [Tooltip("Height of the courier's feet above the road while flying. Towers are 3.4 m, treants 3.6 m.")]
        public float flightHeight = 6f;
        [Tooltip("Chance that a wings pickup replaces the next obstacle pattern once allowed.")]
        [Range(0f, 1f)] public float wingsSpawnChance = 0.3f;
        [Tooltip("Minimum distance between two wings pickups.")]
        public float wingsMinGap = 260f;
        [Tooltip("Air coins switch lanes this often during a flight (meters).")]
        public float airCoinLaneSwitch = 24f;
        [Tooltip("Seconds added to the flight per purchased upgrade.")]
        public float flightUpgradeSeconds = 1.5f;
        [Tooltip("Price of the first flight upgrade; each next level costs twice as much.")]
        public int flightUpgradeBaseCost = 200;
        public int flightUpgradeMaxLevel = 3;

        private static GameConfig instance;

        public static GameConfig Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = Resources.Load<GameConfig>("GameConfig");
                if (instance == null) instance = CreateInstance<GameConfig>();
                return instance;
            }
        }
    }
}
