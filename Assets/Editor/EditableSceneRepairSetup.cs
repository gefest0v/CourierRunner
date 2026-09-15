using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner.Editor
{
    [InitializeOnLoad]
    public static class EditableSceneRepairSetup
    {
        private const string ScenePath = "Assets/Scenes/CourierLevel.unity";
        private const string SessionKey = "CourierRunner.EditableSceneRepair.v2";

        static EditableSceneRepairSetup() => EditorApplication.delayCall += Repair;

        [MenuItem("Courier Runner/Repair Editable Level Runtime")]
        private static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(ScenePath) ||
                SessionState.GetBool(SessionKey, false)) return;

            EditorSceneManager.SaveOpenScenes();
            Scene original = SceneManager.GetActiveScene();
            bool alreadyOpen = original.path == ScenePath;
            Scene level = alreadyOpen ? original : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            GameObject levelRoot = null;
            int removed = 0;
            foreach (GameObject root in level.GetRootGameObjects())
            {
                if (root.name == "Courier Runner Level") levelRoot = root;
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item.gameObject);
                    MonoBehaviour[] oldRuntimeComponents = item.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour behaviour in oldRuntimeComponents)
                    {
                        if (behaviour == null || behaviour is EditableLevelRuntime) continue;
                        if (behaviour.GetType().Namespace == "CourierRunner")
                        {
                            Object.DestroyImmediate(behaviour);
                            removed++;
                        }
                    }
                }
            }

            if (levelRoot != null && levelRoot.GetComponent<EditableLevelRuntime>() == null)
                levelRoot.AddComponent<EditableLevelRuntime>();
            EditorSceneManager.SaveScene(level);
            if (!alreadyOpen) EditorSceneManager.CloseScene(level, true);
            SessionState.SetBool(SessionKey, true);
            Debug.Log($"CourierLevel runtime repaired; removed {removed} broken component references.");
        }
    }
}
