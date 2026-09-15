using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner
{
    /// <summary>
    /// Continuous depth fog. The camera follows the courier, so the clear viewing
    /// radius follows them as well; no fixed world-space fog geometry is used.
    /// </summary>
    public sealed class CourierDistanceFog : MonoBehaviour
    {
        private bool previousEnabled;
        private FogMode previousMode;
        private Color previousColor;
        private float previousStart;
        private float previousEnd;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.Contains("CourierLevel")) return;
            if (FindFirstObjectByType<CourierDistanceFog>() != null) return;
            new GameObject("Courier Distance Fog").AddComponent<CourierDistanceFog>();
        }

        private void Awake()
        {
            previousEnabled = RenderSettings.fog;
            previousMode = RenderSettings.fogMode;
            previousColor = RenderSettings.fogColor;
            previousStart = RenderSettings.fogStartDistance;
            previousEnd = RenderSettings.fogEndDistance;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.65f, 0.69f, 1f);
            RenderSettings.fogStartDistance = 32f;
            RenderSettings.fogEndDistance = 105f;
        }

        private void OnDestroy()
        {
            RenderSettings.fog = previousEnabled;
            RenderSettings.fogMode = previousMode;
            RenderSettings.fogColor = previousColor;
            RenderSettings.fogStartDistance = previousStart;
            RenderSettings.fogEndDistance = previousEnd;
        }
    }
}
