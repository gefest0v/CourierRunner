using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner
{
    public enum RunMode { Story, Endless }

    public static class LevelIds
    {
        public const string CrystalMaiden = "crystal_maiden";
    }

    /// <summary>
    /// Carries the requested run across the scene reload that resets the level.
    /// Replaces the old DontDestroyOnLoad launch-marker object.
    /// </summary>
    public static class GameSession
    {
        private static bool hasPendingRun;
        private static RunMode pendingMode;
        private static string pendingLevelId;

        public static RunMode Mode { get; private set; } = RunMode.Story;
        public static string LevelId { get; private set; } = LevelIds.CrystalMaiden;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            hasPendingRun = false;
            Mode = RunMode.Story;
            LevelId = LevelIds.CrystalMaiden;
        }

        /// <summary>Reloads the game scene and starts the given run as soon as it is built.</summary>
        public static void StartRun(RunMode mode, string levelId = LevelIds.CrystalMaiden)
        {
            hasPendingRun = true;
            pendingMode = mode;
            pendingLevelId = levelId;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>True once after <see cref="StartRun"/>; the scene then skips the main menu.</summary>
        public static bool ConsumePendingRun()
        {
            if (!hasPendingRun) return false;
            hasPendingRun = false;
            Mode = pendingMode;
            LevelId = pendingLevelId;
            return true;
        }
    }
}
