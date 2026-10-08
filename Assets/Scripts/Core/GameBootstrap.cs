using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner
{
    /// <summary>
    /// Builds the game in code after a scene loads, so the scene itself only needs a camera and a light.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildIfMissing();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BuildIfMissing();

        private static void BuildIfMissing()
        {
            if (Object.FindAnyObjectByType<GameWorld>() != null) return;
            GameWorld.Build();
        }
    }
}
