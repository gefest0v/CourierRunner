using UnityEngine;

namespace CourierRunner
{
    public sealed class TowerProjectile : MonoBehaviour
    {
        public float speed = 13f;
        public float maxDistance = 16f;
        private Vector3 startPosition;

        public static TowerProjectile Spawn(Vector3 position)
        {
            GameObject shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shot.name = "Tower Fireball";
            shot.transform.position = position;
            shot.transform.localScale = Vector3.one * 0.62f;
            shot.GetComponent<Collider>().isTrigger = true;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color fireColor = new Color(1f, 0.22f, 0.015f);
            material.SetColor("_BaseColor", new Color(1f, 0.08f, 0.01f));
            material.SetColor("_EmissionColor", fireColor * 5f);
            material.EnableKeyword("_EMISSION");
            shot.GetComponent<Renderer>().material = material;

            TrailRenderer trail = shot.AddComponent<TrailRenderer>();
            trail.time = 0.32f;
            trail.startWidth = 0.48f;
            trail.endWidth = 0.04f;
            trail.minVertexDistance = 0.05f;
            trail.startColor = new Color(1f, 0.65f, 0.05f, 0.9f);
            trail.endColor = new Color(0.8f, 0.02f, 0f, 0f);
            var trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            trailMaterial.SetColor("_BaseColor", new Color(1f, 0.12f, 0.01f, 0.85f));
            trail.material = trailMaterial;

            Light glow = new GameObject("Fire Glow").AddComponent<Light>();
            glow.transform.SetParent(shot.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.28f, 0.03f);
            glow.intensity = 2.5f;
            glow.range = 4f;
            return shot.AddComponent<TowerProjectile>();
        }

        private void Start() => startPosition = transform.position;

        private void Update()
        {
            transform.position += Vector3.back * (speed * Time.deltaTime);
            transform.Rotate(160f * Time.deltaTime, 220f * Time.deltaTime, 0f);
            if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner == null) return;
            runner.Crash();
            Destroy(gameObject);
        }
    }
}
