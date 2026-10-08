using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner
{
    public static class PrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CreatePrototype();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CreatePrototype();

        private static void CreatePrototype()
        {
            EditableLevelRuntime editableLevel = Object.FindAnyObjectByType<EditableLevelRuntime>();
            if (editableLevel != null)
            {
                editableLevel.InitializeNow();
                return;
            }
            if (Object.FindAnyObjectByType<RunnerController>() != null)
                return;

            var world = new GameObject("Courier Runner Prototype");
            var builder = world.AddComponent<PrototypeWorld>();
            builder.Build();
        }
    }
}
