using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner.Editor
{
    [InitializeOnLoad]
    public static class PaintedHorizonSetup
    {
        private const string ScenePath = "Assets/Scenes/CourierLevel.unity";
        private const string RootName = "Painted Horizon Background";
        private static readonly string[] LayerNames =
        {
            "01 Near Forest Layer (Editable)",
            "02 Middle Forest Layer (Editable)",
            "03 Far Forest Layer (Editable)"
        };

        static PaintedHorizonSetup()
        {
            // The background is hand-positioned in CourierLevel. Do not rebuild it
            // automatically, because that would erase the artist's layout work.
        }

        [MenuItem("Courier Runner/Restore Editable Far Background")]
        private static void RestoreFromMenu() => EnsureSingleEditableLayer();

        private static void EnsureSingleEditableLayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene previous = SceneManager.GetActiveScene();
            bool alreadyOpen = previous.path == ScenePath;
            Scene scene = alreadyOpen ? previous : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);
            if (root != null && root.transform.childCount == LayerNames.Length &&
                LayerNames.All(name => root.transform.Cast<Transform>().Any(child => child.name == name)))
            {
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
                return;
            }
            if (root != null) Object.DestroyImmediate(root);

            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            CreateLayer(root.transform, 0, "Assets/Art/ForestCards/ForestHorizonNear.png",
                new Vector3(0f, 13.5f, 285f), 105f, new Color(0.82f, 0.84f, 0.82f, 1f));
            CreateLayer(root.transform, 1, "Assets/Art/ForestCards/ForestHorizonMiddle.png",
                new Vector3(0f, 16.5f, 305f), 116f, new Color(0.74f, 0.79f, 0.79f, 0.94f));
            CreateLayer(root.transform, 2, "Assets/Art/ForestCards/ForestHorizonFar.png",
                new Vector3(0f, 19.5f, 325f), 128f, new Color(0.68f, 0.75f, 0.78f, 0.88f));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Three editable transparent forest layers installed; no parallax movement yet.");
        }

        private static void CreateLayer(Transform parent, int index, string texturePath,
            Vector3 position, float width, Color tint)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Shader shader = Shader.Find("CourierRunner/ForestBackdrop");
            if (texture == null || shader == null) return;

            string materialPath = $"Assets/Resources/Environment/EditableForestLayer{index + 1}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_RemoveChecker", 0f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = LayerNames[index];
            SceneManager.MoveGameObjectToScene(quad, parent.gameObject.scene);
            quad.transform.SetParent(parent, false);
            quad.transform.position = position;
            quad.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(width, width * texture.height / texture.width, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
        }
    }
}
