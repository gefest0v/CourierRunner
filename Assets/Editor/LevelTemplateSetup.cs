using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner.Editor
{
    [InitializeOnLoad]
    public static class LevelTemplateSetup
    {
        private const string SourceScene = "Assets/Scenes/CourierLevel.unity";
        private const string TemplateFolder = "Assets/Scenes/Templates";
        private const string TemplateScene = TemplateFolder + "/ForestLevelTemplate.unity";

        static LevelTemplateSetup() => EditorApplication.delayCall += CreateOnce;

        private static void CreateOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(TemplateScene)) return;
            if (!File.Exists(SourceScene))
            {
                EditorApplication.delayCall += CreateOnce;
                return;
            }
            CreateTemplate(false);
        }

        [MenuItem("Courier Runner/Update Forest Level Template")]
        private static void UpdateTemplate() => CreateTemplate(true);

        private static void CreateTemplate(bool overwrite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.SaveOpenScenes();
            EnsureFolder("Assets/Scenes", "Templates");

            if (overwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScene) != null)
                AssetDatabase.DeleteAsset(TemplateScene);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScene) != null) return;
            if (!AssetDatabase.CopyAsset(SourceScene, TemplateScene))
            {
                Debug.LogError("Could not create the forest level template.");
                return;
            }

            Scene source = SceneManager.GetActiveScene();
            bool sourceIsTemplate = source.path == TemplateScene;
            Scene template = sourceIsTemplate ? source : EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Additive);
            RemoveLevelSpecificObjects(template);
            EditorSceneManager.SaveScene(template);
            if (!sourceIsTemplate) EditorSceneManager.CloseScene(template, true);
            AssetDatabase.SaveAssets();
            Debug.Log("ForestLevelTemplate created without obstacles, pickups or Crystal Maiden. CourierLevel was preserved.");
        }

        private static void RemoveLevelSpecificObjects(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] objects = root.GetComponentsInChildren<Transform>(true);
                for (int i = objects.Length - 1; i >= 0; i--)
                {
                    Transform item = objects[i];
                    if (item == null || item == root.transform) continue;
                    string name = item.name.ToLowerInvariant();
                    bool gameplayGroup = item.name == "02 Obstacles and Pickups";
                    bool recipient = name.Contains("crystal") || name.Contains("recipient");
                    if (gameplayGroup || recipient)
                        Object.DestroyImmediate(item.gameObject);
                }
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
