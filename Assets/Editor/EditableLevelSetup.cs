using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourierRunner.Editor
{
    [InitializeOnLoad]
    public static class EditableLevelSetup
    {
        private const string ScenePath = "Assets/Scenes/CourierLevel.unity";

        static EditableLevelSetup() => EditorApplication.delayCall += CreateOnce;

        [MenuItem("Courier Runner/Rebuild Editable Level")]
        public static void RebuildFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            BuildScene(true);
        }

        private static void CreateOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                return;
            }
            if (File.Exists(ScenePath)) return;
            if (Resources.Load<GameObject>("CourierVisual") == null ||
                Resources.Load<Material>("Environment/Path") == null)
            {
                EditorApplication.delayCall += CreateOnce;
                return;
            }
            BuildScene(false);
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += CreateOnce;
        }

        private static void BuildScene(bool replaceExisting)
        {
            if (!replaceExisting && File.Exists(ScenePath)) return;

            EditorSceneManager.SaveOpenScenes();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject world = new("Courier Runner Level");
            PrototypeWorld builder = world.AddComponent<PrototypeWorld>();
            builder.Build();
            OrganizeHierarchy(world.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetAsBuildScene();
            Selection.activeGameObject = world;
            Debug.Log("Editable CourierLevel scene created. Move decorations directly in the Scene view.");
        }

        private static void OrganizeHierarchy(Transform world)
        {
            Transform environment = NewGroup("01 Environment", world);
            Transform gameplay = NewGroup("02 Obstacles and Pickups", world);
            Transform characters = NewGroup("03 Characters", world);
            Transform systems = NewGroup("04 Camera and UI", world);

            Transform[] children = world.Cast<Transform>()
                .Where(child => child != environment && child != gameplay && child != characters && child != systems)
                .ToArray();

            foreach (Transform child in children)
            {
                string name = child.name.ToLowerInvariant();
                Transform parent = environment;
                if (name.Contains("courier") || name.Contains("crystal") || name.Contains("recipient")) parent = characters;
                else if (name.Contains("camera") || name.Contains("game ui")) parent = systems;
                else if (name.Contains("wall") || name.Contains("jump") || name.Contains("tower") ||
                         name.Contains("gold") || name.Contains("finish") || name.Contains("hitbox")) parent = gameplay;
                child.SetParent(parent, true);
            }
        }

        private static Transform NewGroup(string name, Transform parent)
        {
            GameObject group = new(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void SetAsBuildScene()
        {
            EditorBuildSettingsScene level = new(ScenePath, true);
            EditorBuildSettings.scenes = new[] { level };
        }
    }
}
