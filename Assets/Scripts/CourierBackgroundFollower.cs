using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner
{
    public sealed class CourierBackgroundFollower : MonoBehaviour
    {
        private Transform courier;
        private Vector3 backgroundStart;
        private Vector3 courierStart;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.Contains("CourierLevel")) return;
            GameObject background = GameObject.Find("Painted Horizon Background");
            if (background == null || background.GetComponent<CourierBackgroundFollower>() != null) return;
            background.AddComponent<CourierBackgroundFollower>();
        }

        private void LateUpdate()
        {
            if (courier == null)
            {
                GameObject found = GameObject.Find("Courier");
                if (found == null) return;
                courier = found.transform;
            }

            if (!initialized)
            {
                backgroundStart = transform.position;
                courierStart = courier.position;
                initialized = true;
            }

            Vector3 travel = courier.position - courierStart;
            transform.position = backgroundStart + new Vector3(travel.x * 0.15f, 0f, travel.z);
        }
    }
}
