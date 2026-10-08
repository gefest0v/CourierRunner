using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Renders 3D character models into textures for dialogue portraits while no 2D portraits exist.
    /// The studio sits far below the level on its own layer and only renders while a dialogue is open.
    /// </summary>
    public sealed class PortraitStudio : MonoBehaviour
    {
        /// <summary>Unused by the game otherwise; the main camera never draws it.</summary>
        public const int Layer = 31;
        private const float FieldOfView = 28f;

        private readonly Dictionary<string, RenderTexture> portraits = new();
        private readonly List<Camera> cameras = new();
        private int slot;

        public static PortraitStudio Create(Transform parent)
        {
            var root = new GameObject("Portrait Studio");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(0f, -400f, 0f);
            Camera main = Camera.main;
            if (main != null) main.cullingMask &= ~(1 << Layer);
            return root.AddComponent<PortraitStudio>();
        }

        /// <summary>Returns a live render of the model, or null if the character has no model.</summary>
        public Texture Portrait(DialogueCharacter character, ModelLibrary library)
        {
            if (string.IsNullOrEmpty(character.model)) return null;
            if (portraits.TryGetValue(character.model, out RenderTexture existing)) return existing;

            // Each model gets its own booth so cameras never see each other's subject.
            var booth = new GameObject(character.model + " Booth").transform;
            booth.SetParent(transform, false);
            booth.localPosition = new Vector3(slot++ * 20f, 0f, 0f);

            GameObject model = character.model switch
            {
                "crystal_maiden" => library.CreateCrystalMaiden(booth, Vector3.zero),
                "courier" => library.CreateCourierVisual(booth),
                "wings" => library.CreateWingsVisual(booth),
                _ => null,
            };
            if (model == null) return null;
            // Face the camera, which looks along +Z from in front of the model.
            if (character.model == "courier") model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            // Wings read best from a three-quarter view, slightly from above.
            if (character.model == "wings") model.transform.localRotation = Quaternion.Euler(-12f, 28f, 0f);
            SetLayer(model.transform, Layer);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(booth.position + Vector3.up, Vector3.one * 2f);
            foreach (Renderer item in renderers) bounds.Encapsulate(item.bounds);

            var texture = new RenderTexture(512, 768, 24, RenderTextureFormat.ARGB32) { name = character.model + " Portrait" };
            var cameraObject = new GameObject("Portrait Camera");
            cameraObject.transform.SetParent(booth, false);
            Camera portraitCamera = cameraObject.AddComponent<Camera>();
            portraitCamera.targetTexture = texture;
            portraitCamera.clearFlags = CameraClearFlags.SolidColor;
            portraitCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            portraitCamera.cullingMask = 1 << Layer;
            // HDR and MSAA buffers drop the alpha channel; without them the background stays transparent.
            portraitCamera.allowHDR = false;
            portraitCamera.allowMSAA = false;
            portraitCamera.fieldOfView = FieldOfView;
            portraitCamera.nearClipPlane = 0.1f;
            portraitCamera.farClipPlane = 30f;

            // Characters: head and shoulders (upper ~45 %). Objects: the whole thing.
            bool wholeObject = character.model == "wings";
            float height = wholeObject ? Mathf.Max(bounds.size.y, bounds.size.x * 0.75f) : bounds.size.y;
            Vector3 focus = wholeObject ? bounds.center : new Vector3(bounds.center.x, bounds.min.y + height * 0.72f, bounds.center.z);
            float distance = height * (wholeObject ? 0.95f : 0.45f) / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            cameraObject.transform.position = focus + new Vector3(0f, height * 0.04f, -distance);
            cameraObject.transform.LookAt(focus);

            var lightObject = new GameObject("Portrait Key Light");
            lightObject.transform.SetParent(booth, false);
            lightObject.transform.position = focus + new Vector3(-1.5f, 1.5f, -distance * 0.7f);
            Light key = lightObject.AddComponent<Light>();
            key.type = LightType.Point;
            key.range = distance * 3f;
            key.intensity = 2.2f;
            key.color = new Color(1f, 0.95f, 0.88f);
            key.cullingMask = 1 << Layer;

            cameras.Add(portraitCamera);
            portraits[character.model] = texture;
            return texture;
        }

        /// <summary>Stops rendering portraits (dialogue closed) or resumes it.</summary>
        public void SetRendering(bool active)
        {
            foreach (Camera item in cameras) item.enabled = active;
        }

        private static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayer(child, layer);
        }

        private void OnDestroy()
        {
            foreach (RenderTexture texture in portraits.Values)
                if (texture != null) texture.Release();
        }
    }
}
