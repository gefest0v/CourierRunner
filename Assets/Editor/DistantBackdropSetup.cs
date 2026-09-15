using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CourierRunner.Editor
{
    [InitializeOnLoad]
    public static class DistantBackdropSetup
    {
        private const string ScenePath = "Assets/Scenes/CourierLevel.unity";
        private const string RootName = "Distant Forest Backdrop";
        static DistantBackdropSetup()
        {
            // Intentionally empty. The generated environment is editable by hand and must
            // never be overwritten merely because scripts recompiled or Play Mode ended.
        }

        [MenuItem("Courier Runner/Rebuild Distant Forest")]
        private static void RebuildFromMenu()
        {
            Configure(true);
        }

        private static void Configure(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!System.IO.File.Exists(ScenePath)) return;

            Scene previous = SceneManager.GetActiveScene();
            bool alreadyOpen = previous.path == ScenePath;
            Scene scene = alreadyOpen ? previous : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject oldRoot = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            List<GameObject> roots = scene.GetRootGameObjects().ToList();
            GameObject pathSource = FindNamed(roots, "Stone Forest Path");
            GameObject grassSource = FindNamed(roots, "Forest Grass");
            List<GameObject> treeSources = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => (item.name.StartsWith("TreePine", StringComparison.Ordinal) ||
                                item.name.StartsWith("TreeOak", StringComparison.Ordinal)) &&
                               item.GetComponent<Renderer>() != null)
                .Select(item => item.gameObject)
                .Take(12)
                .ToList();

            if (pathSource == null || grassSource == null || treeSources.Count == 0)
            {
                Debug.LogWarning("Distant forest setup: path, grass or tree sources were not found.");
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            // Cover the full portrait-camera frustum so gray world space never appears beyond
            // the outer forest cards.
            Vector3 grassScale = grassSource.transform.localScale;
            grassSource.transform.localScale = new Vector3(80f, grassScale.y, grassScale.z);

            GameObject backdrop = new(RootName);
            SceneManager.MoveGameObjectToScene(backdrop, scene);
            CreateRoadContinuation(pathSource, grassSource, backdrop.transform);

            var random = new System.Random(73194);
            CreatePointForestCards(backdrop.transform, random);
            CreateSideLayer("Near Forest", treeSources, backdrop.transform, random, 166f, 212f, 8, 7.5f, 17f, 0.9f, 1.18f);
            CreateSideLayer("Middle Forest", treeSources, backdrop.transform, random, 205f, 258f, 10, 8.5f, 21f, 0.72f, 0.98f);
            CreateHorizonLayer(treeSources, backdrop.transform, random);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Distant forest setup complete: outer point cards and road continuation; painted horizon removed.");
        }

        private static void CreatePointForestCards(Transform parent, System.Random random)
        {
            string[] textures = Enumerable.Range(1, 6)
                .Select(index => $"Assets/Art/ForestCards/PointForest{index:00}.png")
                .ToArray();
            Transform root = new GameObject("Point Forest Images").transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, parent.gameObject.scene);
            root.SetParent(parent, false);

            float[] leftZ = { 8f, 22f, 38f, 55f, 73f, 91f, 111f, 132f, 153f };
            float[] rightZ = { 14f, 31f, 47f, 64f, 82f, 101f, 121f, 142f, 160f };
            CreatePointSide(-1, leftZ, textures, root, random);
            CreatePointSide(1, rightZ, textures, root, random);
        }

        private static void CreatePointSide(int side, float[] positions, string[] textures,
            Transform parent, System.Random random)
        {
            for (int index = 0; index < positions.Length; index++)
            {
                float x = side * Mathf.Lerp(22f, 27f, (float)random.NextDouble());
                float z = positions[index] + Mathf.Lerp(-1.2f, 1.2f, (float)random.NextDouble());
                float width = Mathf.Lerp(7.2f, 9f, (float)random.NextDouble());
                CreateForestQuad($"Point Forest {side} {index + 1}", textures[(index * 2 + (side > 0 ? 1 : 0)) % textures.Length],
                    parent, new Vector3(x, width * 0.405f, z), width, true, false,
                    Color.Lerp(Color.white, new Color(0.82f, 0.9f, 0.86f, 1f), (float)random.NextDouble() * 0.25f));
            }
        }

        private static void CreateForestQuad(string name, string texturePath, Transform parent,
            Vector3 position, float width, bool followCourier, bool removeChecker, Color tint)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Shader shader = Shader.Find("CourierRunner/ForestBackdrop");
            if (texture == null || shader == null) return;
            string materialPath = $"Assets/Resources/Environment/{name.Replace(" ", string.Empty)}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_RemoveChecker", removeChecker ? 1f : 0f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);

            Transform pivot = new GameObject(name).transform;
            SceneManager.MoveGameObjectToScene(pivot.gameObject, parent.gameObject.scene);
            pivot.SetParent(parent, false);
            pivot.position = position;
            if (followCourier) pivot.gameObject.AddComponent<ForestBillboard>();
            else pivot.rotation = Quaternion.Euler(0f, 180f, 0f);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Image";
            SceneManager.MoveGameObjectToScene(quad, parent.gameObject.scene);
            quad.transform.SetParent(pivot, false);
            float height = width * texture.height / texture.width;
            quad.transform.localScale = new Vector3(width, height, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            RemoveColliders(quad);
        }

        private static void CreateRoadContinuation(GameObject pathSource, GameObject grassSource, Transform parent)
        {
            GameObject grass = UnityEngine.Object.Instantiate(grassSource, parent);
            grass.name = "Distant Forest Grass";
            grass.transform.position = new Vector3(0f, -0.34f, 245f);
            grass.transform.rotation = grassSource.transform.rotation;
            grass.transform.localScale = new Vector3(grassSource.transform.lossyScale.x,
                grassSource.transform.lossyScale.y, 110f);
            RemoveColliders(grass);

            GameObject road = UnityEngine.Object.Instantiate(pathSource, parent);
            road.name = "Distant Stone Path";
            road.transform.position = new Vector3(0f, -0.255f, 245f);
            road.transform.rotation = pathSource.transform.rotation;
            road.transform.localScale = new Vector3(pathSource.transform.lossyScale.x,
                pathSource.transform.lossyScale.y, 110f);
            RemoveColliders(road);
        }

        private static void CreateSideLayer(string name, List<GameObject> sources, Transform parent,
            System.Random random, float zMin, float zMax, int perSide, float xMin, float xMax,
            float scaleMin, float scaleMax)
        {
            Transform layer = new GameObject(name).transform;
            layer.SetParent(parent, false);
            for (int side = -1; side <= 1; side += 2)
                for (int index = 0; index < perSide; index++)
                {
                    float progress = (index + (float)random.NextDouble() * 0.6f) / perSide;
                    float z = Mathf.Lerp(zMin, zMax, progress);
                    float x = side * Mathf.Lerp(xMin, xMax, (float)random.NextDouble());
                    SpawnTree(sources[random.Next(sources.Count)], layer, random,
                        new Vector3(x, 0f, z), Mathf.Lerp(scaleMin, scaleMax, (float)random.NextDouble()));
                }
        }

        private static void CreateHorizonLayer(List<GameObject> sources, Transform parent, System.Random random)
        {
            Transform layer = new GameObject("Horizon Forest").transform;
            layer.SetParent(parent, false);
            const int count = 24;
            for (int index = 0; index < count; index++)
            {
                float x = Mathf.Lerp(-29f, 29f, index / (count - 1f)) + Mathf.Lerp(-1.1f, 1.1f, (float)random.NextDouble());
                float z = Mathf.Lerp(263f, 292f, (float)random.NextDouble());
                float scale = Mathf.Lerp(0.55f, 0.78f, (float)random.NextDouble());
                SpawnTree(sources[random.Next(sources.Count)], layer, random, new Vector3(x, 0f, z), scale);
            }
        }

        private static void SpawnTree(GameObject source, Transform parent, System.Random random,
            Vector3 position, float scaleFactor)
        {
            GameObject tree = UnityEngine.Object.Instantiate(source, parent);
            tree.name = "Backdrop " + (source.name.StartsWith("TreePine") ? "Pine" : "Oak");
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.AngleAxis((float)random.NextDouble() * 360f, Vector3.up) * source.transform.rotation;
            tree.transform.localScale = source.transform.lossyScale * scaleFactor;
            RemoveColliders(tree);
            foreach (Renderer renderer in tree.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void RemoveColliders(GameObject target)
        {
            foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
        }

        private static GameObject FindNamed(IEnumerable<GameObject> roots, string objectName)
        {
            foreach (GameObject root in roots)
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == objectName) return item.gameObject;
            return null;
        }
    }
}
