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
        public float startSpeed = 9f;
        public float laneWidth = 3f;
        public float laneChangeSpeed = 12f;
        public float jumpHeight = 2.15f;
        [Tooltip("Minimum swipe length in pixels before it counts as a lane change or jump.")]
        public float swipeThreshold = 40f;

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

        [Header("Backdrop")]
        [Tooltip("Distance from the camera to the painted forest horizon.")]
        public float backdropDistance = 80f;

        [Header("Economy")]
        [Tooltip("Keep the coins collected in a failed run. The design doc recommends true for the prototype.")]
        public bool keepCoinsOnFail = true;
        public int wingsPrice = 300;

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
