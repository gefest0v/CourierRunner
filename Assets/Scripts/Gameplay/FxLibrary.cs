using UnityEngine;
using UnityEngine.Rendering;

namespace CourierRunner
{
    /// <summary>Shared procedural textures, materials and particle systems for effects.</summary>
    public static class FxLibrary
    {
        private static Texture2D softCircle;
        private static Material additive;
        private static Material alphaBlended;

        /// <summary>White radial gradient used by every particle.</summary>
        public static Texture2D SoftCircle
        {
            get
            {
                if (softCircle != null) return softCircle;
                const int size = 64;
                softCircle = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Soft Circle" };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f;
                        float dy = (y + 0.5f) / size * 2f - 1f;
                        float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                    }
                softCircle.SetPixels32(pixels);
                softCircle.Apply();
                return softCircle;
            }
        }

        /// <summary>Glowing particles: colors add up, so overlapping flames get brighter.</summary>
        public static Material Additive => additive != null ? additive : additive = CreateParticleMaterial(true);

        /// <summary>Smoke and dust: normal transparency.</summary>
        public static Material AlphaBlended => alphaBlended != null ? alphaBlended : alphaBlended = CreateParticleMaterial(false);

        private static Material CreateParticleMaterial(bool glow)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", SoftCircle);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", glow ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(glow ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        public static Material Emissive(Color baseColor, Color emission)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
            return material;
        }

        /// <summary>A particle system with sensible defaults for short effects; tweak the returned modules.</summary>
        public static ParticleSystem CreateSystem(Transform parent, string name, bool glow, int maxParticles)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = glow ? Additive : AlphaBlended;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        public static Gradient Fade(Color start, Color middle, Color end, float middleAt = 0.4f)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(middle, middleAt), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, middleAt), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            softCircle = null;
            additive = null;
            alphaBlended = null;
        }
    }
}
