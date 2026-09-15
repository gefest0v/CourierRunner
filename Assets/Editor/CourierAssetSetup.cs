using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CourierRunner.Editor
{
    // Rebuilds the runtime courier prefab from the exported Blender asset.
    [InitializeOnLoad]
    public static class CourierAssetSetup
    {
        private const string SourcePath = "Assets/Art/Courier/greevil.fbx";
        private const string ControllerPath = "Assets/Art/Courier/Courier.controller";
        private const string PrefabPath = "Assets/Resources/CourierVisual.prefab";
        private const string TextureFolder = "Assets/Art/Courier/Textures/";
        private const string MaterialFolder = "Assets/Art/Courier/Materials";
        private const string SessionKey = "CourierRunner.AssetSetup.Completed.v5";

        static CourierAssetSetup()
        {
            EditorApplication.delayCall += Configure;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += Configure;
            };
        }

        private static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (SessionState.GetBool(SessionKey, false) ||
                AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) == null)
                return;

            ModelImporter importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter;
            if (importer == null) return;

            bool requiresImport = importer.animationType != ModelImporterAnimationType.Generic ||
                                  !importer.importAnimation;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;

            ModelImporterClipAnimation[] importedClips = importer.defaultClipAnimations;
            if (importedClips.Length > 0)
            {
                foreach (ModelImporterClipAnimation clip in importedClips)
                {
                    bool shouldLoop = Contains(clip.name, "run") || Contains(clip.name, "idle");
                    clip.loopTime = shouldLoop;
                    clip.loopPose = shouldLoop;
                }

                ModelImporterClipAnimation[] currentClips = importer.clipAnimations;
                bool loopSettingsDiffer = currentClips.Length != importedClips.Length ||
                    importedClips.Any(expected => !currentClips.Any(current =>
                        current.name == expected.name && current.loopTime == expected.loopTime));
                if (loopSettingsDiffer)
                {
                    importer.clipAnimations = importedClips;
                    requiresImport = true;
                }
            }

            if (requiresImport)
            {
                importer.SaveAndReimport();
                EditorApplication.delayCall += Configure;
                return;
            }

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            ModelImporterClipAnimation[] configuredClips = importer.clipAnimations;
            bool needsExplicitClips = configuredClips.Length != clips.Length || clips.Any(sourceClip =>
                !configuredClips.Any(configured => configured.name == sourceClip.name &&
                    configured.loopTime == (Contains(sourceClip.name, "run") || Contains(sourceClip.name, "idle"))));
            if (clips.Length > 0 && needsExplicitClips)
            {
                importer.clipAnimations = clips.Select(sourceClip =>
                {
                    bool shouldLoop = Contains(sourceClip.name, "run") || Contains(sourceClip.name, "idle");
                    return new ModelImporterClipAnimation
                    {
                        name = sourceClip.name,
                        takeName = sourceClip.name,
                        firstFrame = 0f,
                        lastFrame = Mathf.Round(sourceClip.length * sourceClip.frameRate),
                        loopTime = shouldLoop,
                        loopPose = shouldLoop,
                        lockRootRotation = true,
                        lockRootHeightY = true,
                        lockRootPositionXZ = true
                    };
                }).ToArray();
                importer.SaveAndReimport();
                EditorApplication.delayCall += Configure;
                return;
            }

            AnimationClip run = FindClip(clips, "miniboss_run", "run");
            AnimationClip idle = FindClip(clips, "turntable_idle", "idle");
            AnimationClip stun = FindClip(clips, "miniboss_stun", "stun", "death");
            if (run == null)
            {
                Debug.LogError("Courier setup: в greevil.fbx не найден клип Run. Найдены: " +
                               string.Join(", ", clips.Select(clip => clip.name)));
                return;
            }

            if (idle == null) idle = run;
            if (stun == null) stun = idle;

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Art/Courier", "Materials");
            Material bodyMaterial = CreateMaterial("CourierBody", "greevil_body_color_psd_eed11e01.png",
                "greevil_body_normal_psd_574e59a6.png");
            Material eyesMaterial = CreateMaterial("CourierEyes", "greevil_eyes_color_psd_4a7cef59.png",
                "greevil_eyes_normal_psd_5cceb878.png");
            Material teethMaterial = CreateMaterial("CourierTeeth", "greevil_teeth_color_psd_2851cdff.png",
                "greevil_teeth_normal_psd_deaa66c1.png");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            else
            {
                AnimatorStateMachine oldMachine = controller.layers[0].stateMachine;
                foreach (ChildAnimatorState child in oldMachine.states)
                    oldMachine.RemoveState(child.state);
                foreach (AnimatorStateTransition transition in oldMachine.anyStateTransitions)
                    oldMachine.RemoveAnyStateTransition(transition);
                controller.parameters = System.Array.Empty<AnimatorControllerParameter>();
            }
            controller.AddParameter("Finished", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Crash", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState runState = machine.AddState("Run");
            AnimatorState idleState = machine.AddState("Idle");
            AnimatorState stunState = machine.AddState("Stun");
            runState.motion = run;
            idleState.motion = idle;
            stunState.motion = stun;
            machine.defaultState = runState;

            AnimatorStateTransition finishTransition = runState.AddTransition(idleState);
            finishTransition.hasExitTime = false;
            finishTransition.duration = 0.15f;
            finishTransition.AddCondition(AnimatorConditionMode.If, 0f, "Finished");

            AnimatorStateTransition crashTransition = machine.AddAnyStateTransition(stunState);
            crashTransition.hasExitTime = false;
            crashTransition.duration = 0.08f;
            crashTransition.AddCondition(AnimatorConditionMode.If, 0f, "Crash");

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            GameObject instance = new GameObject("CourierVisual");
            GameObject model = UnityEngine.Object.Instantiate(source, instance.transform);
            model.name = "Greevil Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            Animator animator = model.GetComponentInChildren<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                string rendererName = renderer.gameObject.name.ToLowerInvariant();
                if (rendererName.Contains("eyes"))
                {
                    renderer.enabled = false;
                    continue;
                }
                else if (rendererName.Contains("teeth")) renderer.sharedMaterial = teethMaterial;
                else renderer.sharedMaterial = bodyMaterial;
            }

            NormalizeSizeAndCenter(model, 1.65f);
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            SessionState.SetBool(SessionKey, true);

            Debug.Log("Courier setup complete. Animation clips: " +
                      string.Join(", ", clips.Select(clip => clip.name)));
        }

        private static AnimationClip FindClip(AnimationClip[] clips, params string[] names)
        {
            foreach (string name in names)
            {
                AnimationClip match = clips.FirstOrDefault(clip =>
                    clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null) return match;
            }
            return null;
        }

        private static bool Contains(string value, string part) =>
            value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        private static Material CreateMaterial(string name, string colorTextureName, string normalTextureName)
        {
            string materialPath = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            bool isNew = material == null;
            if (isNew) material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            else material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.name = name;
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + colorTextureName);
            Texture2D normal = LoadNormalTexture(TextureFolder + normalTextureName);
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.25f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (isNew) AssetDatabase.CreateAsset(material, materialPath);
            else EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadNormalTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void NormalizeSizeAndCenter(GameObject instance, float targetHeight)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.y <= 0.001f) return;

            float scale = targetHeight / bounds.size.y;
            instance.transform.localScale = Vector3.one * scale;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            instance.transform.position += Vector3.up * -bounds.center.y;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
