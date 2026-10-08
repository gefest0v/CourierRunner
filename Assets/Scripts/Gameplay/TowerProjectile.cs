using UnityEngine;

namespace CourierRunner
{
    /// <summary>Pooled fireball that flies toward the courier along the tower's lane.</summary>
    public sealed class TowerProjectile : MonoBehaviour, IPoolable
    {
        public float speed = 13f;
        public float maxDistance = 16f;
        private float travelled;
        private TrailRenderer trail;

        public static GameObject CreateTemplate()
        {
            GameObject shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shot.name = "Tower Fireball";
            shot.transform.localScale = Vector3.one * 0.62f;
            shot.GetComponent<Collider>().isTrigger = true;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color fireColor = new Color(1f, 0.22f, 0.015f);
            material.SetColor("_BaseColor", new Color(1f, 0.08f, 0.01f));
            material.SetColor("_EmissionColor", fireColor * 5f);
            material.EnableKeyword("_EMISSION");
            shot.GetComponent<Renderer>().sharedMaterial = material;

            TrailRenderer trail = shot.AddComponent<TrailRenderer>();
            trail.time = 0.32f;
            trail.startWidth = 0.48f;
            trail.endWidth = 0.04f;
            trail.minVertexDistance = 0.05f;
            trail.startColor = new Color(1f, 0.65f, 0.05f, 0.9f);
            trail.endColor = new Color(0.8f, 0.02f, 0f, 0f);
            var trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            trailMaterial.SetColor("_BaseColor", new Color(1f, 0.12f, 0.01f, 0.85f));
            trail.sharedMaterial = trailMaterial;

            Light glow = new GameObject("Fire Glow").AddComponent<Light>();
            glow.transform.SetParent(shot.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.28f, 0.03f);
            glow.intensity = 2.5f;
            glow.range = 4f;
            shot.AddComponent<TowerProjectile>();
            return shot;
        }

        private void Awake() => trail = GetComponent<TrailRenderer>();

        public void OnTakenFromPool()
        {
            travelled = 0f;
            if (trail != null) trail.Clear();
        }

        public void OnReturnedToPool() { }

        /// <summary>Called after the world was shifted so the trail does not streak across the jump.</summary>
        public void ClearTrail()
        {
            if (trail != null) trail.Clear();
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;
            transform.position += Vector3.back * step;
            transform.Rotate(160f * Time.deltaTime, 220f * Time.deltaTime, 0f);
            travelled += step;
            if (travelled >= maxDistance) PooledObject.ReleaseOrDestroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner == null) return;
            runner.Crash();
            PooledObject.ReleaseOrDestroy(gameObject);
        }
    }
}
