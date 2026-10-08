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
