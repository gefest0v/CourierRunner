using UnityEngine;

namespace CourierRunner
{
    /// <summary>Follows the courier and letterboxes the view to a 9:16 phone frame.</summary>
    public sealed class RunnerCamera : MonoBehaviour
    {
        public Transform target;
        private readonly Vector3 offset = new(0f, 5f, -8f);
        private Camera gameCamera;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private float trauma;
        private float heldTrauma;

        private void Awake()
        {
            gameCamera = GetComponent<Camera>();
            ApplyPhoneViewport();
        }

        private void Update()
        {
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
                ApplyPhoneViewport();
        }

        private void ApplyPhoneViewport()
        {
            if (gameCamera == null || Screen.width <= 0 || Screen.height <= 0) return;

            const float targetAspect = 9f / 16f;
            float screenAspect = (float)Screen.width / Screen.height;
            Rect viewport;
            if (screenAspect > targetAspect)
            {
                float width = targetAspect / screenAspect;
                viewport = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
            else
            {
                float height = screenAspect / targetAspect;
                viewport = new Rect(0f, (1f - height) * 0.5f, 1f, height);
            }

            gameCamera.rect = viewport;
            gameCamera.backgroundColor = new Color(0.42f, 0.56f, 0.67f, 1f);
            gameCamera.clearFlags = CameraClearFlags.Skybox;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }

        /// <summary>A one-off jolt (0..1) that fades out over about half a second.</summary>
        public void AddShake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        /// <summary>A continuous tremble for this frame only; call every frame while it should last.</summary>
        public void HoldShake(float amount) => heldTrauma = Mathf.Max(heldTrauma, amount);

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, target.position + offset, 8f * Time.deltaTime);
            transform.LookAt(target.position + new Vector3(0f, 1f, 4f));

            float strength = Mathf.Max(trauma, heldTrauma);
            heldTrauma = 0f;
            trauma = Mathf.Max(0f, trauma - Time.deltaTime * 2f);
            if (strength <= 0f) return;
            // Squared so small values stay subtle; Perlin noise keeps the motion smooth.
            float amplitude = strength * strength;
            float time = Time.time * 25f;
            transform.position += new Vector3(Mathf.PerlinNoise(time, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, time) - 0.5f, 0f) * amplitude * 1.2f;
            transform.Rotate(0f, 0f, (Mathf.PerlinNoise(time, time * 0.5f) - 0.5f) * amplitude * 6f);
        }
    }
}
