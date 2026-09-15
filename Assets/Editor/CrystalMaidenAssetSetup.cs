using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CourierRunner.Editor
{
    [InitializeOnLoad]
    public static class CrystalMaidenAssetSetup
    {
        private const string ModelPath = "Assets/Resources/Heroes/CrystalMaiden.fbx";
        private const string ControllerPath = "Assets/Resources/Heroes/CrystalMaidenIdle.controller";

        static CrystalMaidenAssetSetup() => EditorApplication.delayCall += Configure;

        private static void Configure()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) return;
            if (importer.animationType != ModelImporterAnimationType.Generic || !importer.importAnimation)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.SaveAndReimport();
                EditorApplication.delayCall += Configure;
                return;
            }

            AnimationClip idle = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                                        (clip.name.IndexOf("CM_Wait", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         clip.name.IndexOf("cm_idle", StringComparison.OrdinalIgnoreCase) >= 0));
            if (idle == null)
            {
                Debug.LogError("Crystal Maiden setup: CM_Wait animation was not imported.");
                return;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("Wait");
            state.motion = idle;
            controller.layers[0].stateMachine.defaultState = state;
            AssetDatabase.SaveAssets();
            Debug.Log("Crystal Maiden setup complete: combined model + cm_idle.");
        }
    }
}
