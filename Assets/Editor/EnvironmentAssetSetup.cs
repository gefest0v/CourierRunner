using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner.Editor
{
    [InitializeOnLoad]
    public static class EnvironmentAssetSetup
    {
        private const string SessionKey = "CourierRunner.EnvironmentSetup.v15";

        static EnvironmentAssetSetup() => EditorApplication.delayCall += Configure;

        private static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                return;
            }
            if (SessionState.GetBool(SessionKey, false)) return;
            bool ready = SavePrefab("Assets/Art/Wall/riveredge_rock005a.gltf", "Assets/Resources/Environment/RockSingle.prefab");
            if (!ready)
            {
                EditorApplication.delayCall += Configure;
                return;
            }
            DeleteAllBushes();
            CreateGroundMaterial();
            CreateGrassMaterial();
            UpdateGroundMaterialsInEditableScene();
            AssetDatabase.SaveAssets();
            SessionState.SetBool(SessionKey, true);
            Debug.Log("Environment setup complete: path and single rock; all bushes removed.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.delayCall += Configure;
        }

        private static void DeleteAllBushes()
        {
            const string scenePath = "Assets/Scenes/CourierLevel.unity";
            if (System.IO.File.Exists(scenePath))
            {
                Scene originalScene = SceneManager.GetActiveScene();
                bool alreadyOpen = originalScene.path == scenePath;
                Scene levelScene = alreadyOpen ? originalScene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                foreach (GameObject root in levelScene.GetRootGameObjects())
                {
                    Transform[] objects = root.GetComponentsInChildren<Transform>(true);
                    for (int i = objects.Length - 1; i >= 0; i--)
                        if (objects[i] != null &&
                            (objects[i].name == "Bush1" || objects[i].name == "Bush2" ||
                             objects[i].name.StartsWith("bush_")))
                            Object.DestroyImmediate(objects[i].gameObject);
                }
                EditorSceneManager.SaveScene(levelScene);
                if (!alreadyOpen) EditorSceneManager.CloseScene(levelScene, true);
            }
            AssetDatabase.DeleteAsset("Assets/Resources/Environment/Bush1.prefab");
            AssetDatabase.DeleteAsset("Assets/Resources/Environment/Bush2.prefab");
            AssetDatabase.DeleteAsset("Assets/Art/Bush");
        }

        private static bool SavePrefab(string sourcePath, string prefabPath)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) return false;
            EnsureFolder("Assets/Resources", "Environment");
            GameObject instance = Object.Instantiate(source);
            instance.name = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return true;
        }

        private static void CreateGroundMaterial()
        {
            EnsureFolder("Assets/Resources", "Environment");
            const string materialPath = "Assets/Resources/Environment/Path.mat";
            Shader shader = Shader.Find("CourierRunner/ForestPathTransition");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            bool isNew = material == null;
            if (isNew) material = new Material(shader);
            else material.shader = shader;
            material.name = "Forest Dirt Path";
            const string texturePath = "Assets/Art/Ground/Path/materials/models/props_generic/ground_generic_treadmil_large_color.png";
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null && importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Texture2D grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Ground/Path/ground_grass.png");
            material.SetTexture("_PathMap", texture);
            material.SetTexture("_GrassMap", grassTexture);
            material.SetColor("_PathTint", new Color(0.82f, 0.84f, 0.76f, 1f));
            material.SetColor("_GrassTint", new Color(0.84f, 0.88f, 0.78f, 1f));
            material.SetFloat("_LengthRepeats", 10f);
            material.SetFloat("_GrassLengthRepeats", 40f);
            material.SetVector("_WorldTiling", new Vector4(0.1f, 0.2f, 0f, 0f));
            if (isNew) AssetDatabase.CreateAsset(material, materialPath);
            else EditorUtility.SetDirty(material);
        }

        private static void CreateGrassMaterial()
        {
            EnsureFolder("Assets/Resources", "Environment");
            const string materialPath = "Assets/Resources/Environment/Grass.mat";
            const string texturePath = "Assets/Art/Ground/Path/ground_grass.png";
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null && importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            bool isNew = material == null;
            Shader shader = Shader.Find("CourierRunner/WorldSpaceGrass");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (isNew) material = new Material(shader);
            else material.shader = shader;
            material.name = "Forest Grass";
            material.SetTexture("_GrassMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_GrassTint", new Color(0.84f, 0.88f, 0.78f, 1f));
            material.SetVector("_WorldTiling", new Vector4(0.1f, 0.2f, 0f, 0f));
            if (isNew) AssetDatabase.CreateAsset(material, materialPath);
            else EditorUtility.SetDirty(material);
        }

        private static void UpdateGroundMaterialsInEditableScene()
        {
            const string scenePath = "Assets/Scenes/CourierLevel.unity";
            if (!System.IO.File.Exists(scenePath)) return;
            Scene originalScene = SceneManager.GetActiveScene();
            bool alreadyOpen = originalScene.path == scenePath;
            Scene levelScene = alreadyOpen ? originalScene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            Material path = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Environment/Path.mat");
            Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Environment/Grass.mat");
            foreach (GameObject root in levelScene.GetRootGameObjects())
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name == "Stone Forest Path") renderer.sharedMaterial = path;
                    else if (renderer.gameObject.name == "Forest Grass") renderer.sharedMaterial = grass;
                }
            EditorSceneManager.SaveScene(levelScene);
            if (!alreadyOpen) EditorSceneManager.CloseScene(levelScene, true);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
