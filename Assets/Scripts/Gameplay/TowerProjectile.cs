using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Pooled fireball that flies toward the courier along the tower's lane: a white-hot core in an
    /// orange halo, a flame-and-smoke tail, a flickering light, and a spark burst when it hits or fades.
    /// </summary>
    public sealed class TowerProjectile : MonoBehaviour, IPoolable
    {
        public float speed = 13f;
        public float maxDistance = 18f;
        private float travelled;
        private Light glow;
        private Transform core;
        private ParticleSystem flames;
        private ParticleSystem smoke;
        private ParticleSystem burst;
        private bool exploding;
        private float explodeTimer;

        public static GameObject CreateTemplate()
        {
            var root = new GameObject("Tower Fireball");
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.4f;

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            Object.DestroyImmediate(core.GetComponent<Collider>());
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = Vector3.one * 0.42f;
            core.GetComponent<Renderer>().sharedMaterial = FxLibrary.Emissive(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.55f, 0.1f) * 6f);

            // Halo: a cloud of large additive particles that stays with the core.
            ParticleSystem haloSystem = FxLibrary.CreateSystem(root.transform, "Halo", true, 12);
            ParticleSystem.MainModule haloMain = haloSystem.main;
            haloMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            haloMain.loop = true;
            haloMain.startLifetime = 0.25f;
            haloMain.startSpeed = 0f;
            haloMain.startSize = new ParticleSystem.MinMaxCurve(1.1f, 1.5f);
            haloMain.startColor = new Color(1f, 0.45f, 0.08f, 0.55f);
            ParticleSystem.EmissionModule haloEmission = haloSystem.emission;
            haloEmission.rateOverTime = 40f;
            ParticleSystem.ShapeModule haloShape = haloSystem.shape;
            haloShape.enabled = false;

            ParticleSystem flameSystem = FxLibrary.CreateSystem(root.transform, "Flames", true, 120);
            ParticleSystem.MainModule flameMain = flameSystem.main;
            flameMain.loop = true;
            flameMain.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            flameMain.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
            flameMain.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            flameMain.startColor = Color.white;
            ParticleSystem.EmissionModule flameEmission = flameSystem.emission;
            flameEmission.rateOverTime = 70f;
            ParticleSystem.ShapeModule flameShape = flameSystem.shape;
            flameShape.shapeType = ParticleSystemShapeType.Sphere;
            flameShape.radius = 0.18f;
            ParticleSystem.ColorOverLifetimeModule flameColor = flameSystem.colorOverLifetime;
            flameColor.enabled = true;
            flameColor.color = FxLibrary.Fade(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.45f, 0.05f), new Color(0.6f, 0.05f, 0f));
            ParticleSystem.SizeOverLifetimeModule flameSize = flameSystem.sizeOverLifetime;
            flameSize.enabled = true;
            flameSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

            ParticleSystem smokeSystem = FxLibrary.CreateSystem(root.transform, "Smoke", false, 60);
            ParticleSystem.MainModule smokeMain = smokeSystem.main;
            smokeMain.loop = true;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            smokeMain.startColor = new Color(0.2f, 0.17f, 0.15f, 0.5f);
            smokeMain.gravityModifier = -0.15f;
            ParticleSystem.EmissionModule smokeEmission = smokeSystem.emission;
            smokeEmission.rateOverTime = 22f;
            ParticleSystem.ColorOverLifetimeModule smokeColor = smokeSystem.colorOverLifetime;
            smokeColor.enabled = true;
            smokeColor.color = FxLibrary.Fade(new Color(0.35f, 0.3f, 0.28f), new Color(0.25f, 0.22f, 0.2f), new Color(0.2f, 0.2f, 0.2f), 0.3f);
            ParticleSystem.SizeOverLifetimeModule smokeSize = smokeSystem.sizeOverLifetime;
            smokeSize.enabled = true;
            smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));

            ParticleSystem burstSystem = FxLibrary.CreateSystem(root.transform, "Burst", true, 40);
            ParticleSystem.MainModule burstMain = burstSystem.main;
            burstMain.loop = false;
            burstMain.duration = 0.2f;
            burstMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            burstMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            burstMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            burstMain.startColor = new Color(1f, 0.7f, 0.2f);
            burstMain.gravityModifier = 0.6f;
            ParticleSystem.EmissionModule burstEmission = burstSystem.emission;
            burstEmission.rateOverTime = 0f;
            burstEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
            ParticleSystem.ShapeModule burstShape = burstSystem.shape;
            burstShape.shapeType = ParticleSystemShapeType.Sphere;
            burstShape.radius = 0.2f;
            ParticleSystem.ColorOverLifetimeModule burstColor = burstSystem.colorOverLifetime;
            burstColor.enabled = true;
            burstColor.color = FxLibrary.Fade(new Color(1f, 0.9f, 0.5f), new Color(1f, 0.4f, 0.05f), new Color(0.5f, 0.05f, 0f));

            Light light = new GameObject("Fire Glow").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.42f, 0.08f);
            light.intensity = 3f;
            light.range = 5f;
            root.AddComponent<TowerProjectile>();
            return root;
        }

        private void Awake()
        {
            glow = GetComponentInChildren<Light>();
            core = transform.Find("Core");
            flames = transform.Find("Flames").GetComponent<ParticleSystem>();
            smoke = transform.Find("Smoke").GetComponent<ParticleSystem>();
            burst = transform.Find("Burst").GetComponent<ParticleSystem>();
        }

        public void OnTakenFromPool()
        {
            travelled = 0f;
            exploding = false;
            if (core != null) core.gameObject.SetActive(true);
            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
            {
                system.Clear(true);
                if (system != burst) system.Play(true);
            }
        }

        public void OnReturnedToPool()
        {
            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Called after the world was shifted so trails do not streak across the jump.</summary>
        public void ClearTrail()
        {
            flames.Clear(true);
            smoke.Clear(true);
        }

        private void Update()
        {
            if (exploding)
            {
                explodeTimer -= Time.deltaTime;
                glow.intensity = Mathf.Max(0f, explodeTimer) * 10f;
                if (explodeTimer <= 0f) PooledObject.ReleaseOrDestroy(gameObject);
                return;
            }
            float step = speed * Time.deltaTime;
            // Launched from the tower's orb, the fireball dips to head height over its first meters.
            Vector3 position = transform.position + Vector3.back * step;
            position.y = Mathf.MoveTowards(position.y, 1.25f, step * 0.6f);
            transform.position = position;
            travelled += step;
            glow.intensity = 2.6f + Mathf.PerlinNoise(Time.time * 18f, travelled) * 1.6f;
            if (travelled >= maxDistance) Explode();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (exploding) return;
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner == null) return;
            runner.Crash();
            Explode();
        }

        /// <summary>The core vanishes in a burst of sparks; the object returns to the pool when they settle.</summary>
        private void Explode()
        {
            exploding = true;
            explodeTimer = 0.6f;
            if (core != null) core.gameObject.SetActive(false);
            flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            transform.Find("Halo").GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            burst.Play(true);
        }
    }
}
