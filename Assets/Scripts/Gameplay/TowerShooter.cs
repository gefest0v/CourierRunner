using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Fires a fireball down its lane while the courier is in the same lane and within range.
    /// A glowing orb on the tower charges up just before each shot so the player can react.
    /// </summary>
    public sealed class TowerShooter : MonoBehaviour, IPoolable
    {
        public const float ChargeTime = 0.45f;
        public float detectionDistance = 20f;
        public float fireInterval = 1.25f;
        private float cooldown = 0.5f;
        private float charge;
        private Transform orb;
        private Light orbLight;
        private ParticleSystem embers;

        /// <summary>Adds the charge orb, its light and idle embers to a tower template.</summary>
        public static void Decorate(GameObject tower)
        {
            var orbObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbObject.name = "Charge Orb";
            Object.DestroyImmediate(orbObject.GetComponent<Collider>());
            orbObject.transform.SetParent(tower.transform, false);
            orbObject.transform.localPosition = new Vector3(0f, 3.05f, -0.9f);
            orbObject.transform.localScale = Vector3.one * 0.35f;
            orbObject.GetComponent<Renderer>().sharedMaterial = FxLibrary.Emissive(new Color(1f, 0.3f, 0.1f), new Color(1f, 0.25f, 0.05f) * 4f);

            Light light = new GameObject("Orb Light").AddComponent<Light>();
            light.transform.SetParent(orbObject.transform, false);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.35f, 0.1f);
            light.range = 6f;
            light.intensity = 0.6f;

            ParticleSystem system = FxLibrary.CreateSystem(tower.transform, "Embers", true, 30);
            system.transform.localPosition = orbObject.transform.localPosition;
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new Color(1f, 0.55f, 0.15f);
            main.gravityModifier = -0.2f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 10f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = FxLibrary.Fade(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.35f, 0.05f), new Color(0.5f, 0.05f, 0f));
        }

        private void Awake()
        {
            orb = transform.Find("Charge Orb");
            orbLight = orb != null ? orb.GetComponentInChildren<Light>() : null;
            Transform embersTransform = transform.Find("Embers");
            embers = embersTransform != null ? embersTransform.GetComponent<ParticleSystem>() : null;
        }

        public void OnTakenFromPool()
        {
            cooldown = 0.5f;
            charge = 0f;
            if (embers != null) embers.Play(true);
            GameWorld world = GameWorld.Current;
            if (world != null && world.Difficulty != null)
                fireInterval = world.Difficulty.TowerFireIntervalAt(world.Root.InverseTransformPoint(transform.position).z);
        }

        public void OnReturnedToPool()
        {
            if (embers != null) embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            GameWorld world = GameWorld.Current;
            if (world == null || world.Runner == null) return;
            RunnerController runner = world.Runner;
            Vector3 runnerPosition = runner.transform.position;
            Vector3 position = transform.position;
            float forwardDistance = position.z - runnerPosition.z;
            bool sameLane = Mathf.Abs(position.x - runnerPosition.x) < 1.15f;
            // Fireballs fly at head height; a flying courier is out of reach.
            bool target = runner.IsRunning && !runner.IsFlying && sameLane &&
                          forwardDistance > 1.5f && forwardDistance <= detectionDistance;

            cooldown -= Time.deltaTime;
            charge = target && cooldown <= ChargeTime ? charge + Time.deltaTime : Mathf.Max(0f, charge - Time.deltaTime * 2f);
            UpdateOrb();
            if (!target || charge < ChargeTime || cooldown > 0f) return;

            cooldown = fireInterval;
            charge = 0f;
            Vector3 spawn = orb != null ? orb.position : new Vector3(position.x, 1.25f, position.z - 1.3f);
            world.Library.Get(PoolKeys.Fireball, world.Root, world.Root.InverseTransformPoint(spawn));
            if (orbLight != null) orbLight.intensity = 8f;
        }

        /// <summary>The orb swells and brightens while charging and idles with a slow pulse.</summary>
        private void UpdateOrb()
        {
            if (orb == null) return;
            float t = Mathf.Clamp01(charge / ChargeTime);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            orb.localScale = Vector3.one * Mathf.Lerp(0.32f + pulse * 0.04f, 0.7f, t);
            if (orbLight != null)
                orbLight.intensity = Mathf.MoveTowards(orbLight.intensity, Mathf.Lerp(0.5f + pulse * 0.4f, 5f, t), Time.deltaTime * 30f);
        }
    }
}
