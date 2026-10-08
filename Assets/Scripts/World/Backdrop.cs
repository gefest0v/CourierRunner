using UnityEngine;
using UnityEngine.Rendering;

namespace CourierRunner
{
    /// <summary>
    /// The painted horizon. It stays at a fixed distance in front of the camera, so it never grows
    /// as the courier runs and never has to be placed along the track.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        private static readonly float[] DepthOffsets = { 0f, 12f, 24f };
        private static readonly float[] Heights = { 0.055f, 0.07f, 0.085f };
        private static readonly float[] Widths = { 0.52f, 0.56f, 0.6f };

        private Transform cameraTransform;

        private void Start()
        {
            float distance = GameConfig.Instance.backdropDistance;
            for (int i = 0; i < DepthOffsets.Length; i++)
            {
                Material material = Resources.Load<Material>($"Environment/EditableForestLayer{i + 1}");
                if (material == null) continue;
                Texture texture = material.GetTexture("_BaseMap");
                float aspect = texture != null ? (float)texture.height / texture.width : 0.3f;

                // Sizes are proportional to the distance so the look stays the same if the distance is tuned.
                float layerDistance = distance + DepthOffsets[i];
                float width = layerDistance * Widths[i] * 2f;
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Horizon Layer {i + 1}";
                Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(transform, false);
                quad.transform.localPosition = new Vector3(0f, layerDistance * Heights[i] + width * aspect * 0.5f - 2f, layerDistance);
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = new Vector3(width, width * aspect, 1f);
                Renderer renderer = quad.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                cameraTransform = main.transform;
            }
            Vector3 position = cameraTransform.position;
            transform.position = new Vector3(position.x, 0f, position.z);
        }
    }
}
