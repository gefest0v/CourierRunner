using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CourierRunner
{
    public static class PoolKeys
    {
        public const string TreeOak = "Tree Oak";
        public const string TreePine = "Tree Pine";
        public const string TreeStump = "Tree Stump";
        public static readonly string[] DecorRocks = { "Decor Rock 1", "Decor Rock 2", "Decor Rock 3" };
        public const string Well = "Well";
        public const string ForestCard = "Forest Card";
        public const string Coin = "Gold";
        /// <summary>Low jumpable obstacles made of wood: a fallen log and a broad stump.</summary>
        public static readonly string[] Walls = { "Fallen Log - Jump", "Stump - Jump" };
        /// <summary>Only the single big boulder: the rock clusters have gaps that a box hitbox cannot follow.</summary>
        public static readonly string[] Rocks = { "Rock - Jump" };
        public const string Tower = "Lane Tower";
        public const string Fireball = "Tower Fireball";
        public const string Treant = "Treant";
        public const string Wings = "Wings Pickup";
    }

    /// <summary>
    /// Turns the imported models in Resources into normalized templates (pivot at the bottom center,
    /// fixed height) and owns one <see cref="ObjectPool"/> per template. Missing models fall back to
    /// colored cubes so the game stays playable while art is being made.
    /// </summary>
    public sealed class ModelLibrary
    {
        public const int ForestCardVariants = 6;
        public static readonly string[] ForestWallMaterials = { "ForestSideDeep", "ForestSideMixed", "ForestSidePine" };
        public static int ForestWallVariants => ForestWallMaterials.Length;

        private readonly Transform templateRoot;
        private readonly Transform storage;
        private readonly Dictionary<string, ObjectPool> pools = new();
        private readonly Dictionary<string, Material> materials = new();

        public ModelLibrary(Transform parent)
        {
            templateRoot = new GameObject("Templates").transform;
            templateRoot.SetParent(parent, false);
            storage = new GameObject("Pool Storage").transform;
            storage.SetParent(parent, false);
            storage.gameObject.SetActive(false);

            Material oak = TextureMaterial("Trees/Textures/tree_oak_leaves_00_color_psd_c6c1c88b", true, 0.12f);
            Material pine = TextureMaterial("Trees/Textures/tree_pine_frond_00_color_psd_cad45fc8", true, 0.12f);
            Material stump = TextureMaterial("Trees/Textures/tree_stump001_color_psd_69850232", true, 0.12f);
            Material well = TextureMaterial("Decorations/well_color", false, 0.18f);
            Material tower = TextureMaterial("Tower/tower_color", false, 0.2f);

            RegisterModel(PoolKeys.TreeOak, "Trees/TreeOak", 5.5f, 0f, oak, false, new Color(0.2f, 0.45f, 0.15f));
            RegisterModel(PoolKeys.TreePine, "Trees/TreePine", 6.2f, 0f, pine, false, new Color(0.12f, 0.35f, 0.18f));
            RegisterModel(PoolKeys.TreeStump, "Trees/TreeStump", 1.25f, 0f, stump, false, new Color(0.4f, 0.28f, 0.16f));
            Material rock = TextureMaterial("Decorations/rock_color", false, 0.16f);
            RegisterModel(PoolKeys.DecorRocks[0], "Decorations/Rock1", 1f, 0f, rock, false, new Color(0.45f, 0.45f, 0.48f));
            RegisterModel(PoolKeys.DecorRocks[1], "Decorations/Rock2", 1f, 0f, rock, false, new Color(0.45f, 0.45f, 0.48f));
            // RockSingle comes from a glTF (Y-up), so it needs no FBX axis correction.
            RegisterModel(PoolKeys.DecorRocks[2], "Environment/RockSingle", 1f, 0f, null, false, new Color(0.45f, 0.45f, 0.48f),
                yUp: true);
            RegisterModel(PoolKeys.Well, "Decorations/Well", 2.2f, 0f, well, false, new Color(0.5f, 0.42f, 0.3f));
            for (int i = 0; i < ForestCardVariants; i++)
            {
                int variant = i;
                Register(ForestCardKey(variant), () => CreateForestCard(variant));
            }
            for (int i = 0; i < ForestWallVariants; i++)
            {
                int variant = i;
                Register(ForestWallKey(variant), () => CreateForestWall(variant));
            }

            Register(PoolKeys.Walls[0], CreateFallenLog(stump));
            RegisterModel(PoolKeys.Walls[1], "Trees/TreeStump", 1.3f, 2.2f, stump, true, new Color(0.4f, 0.28f, 0.16f));
            RegisterModel(PoolKeys.Rocks[0], "Environment/RockSingle", 1.25f, 2.3f, null, true, new Color(0.45f, 0.45f, 0.48f),
                yUp: true);
            RegisterModel(PoolKeys.Tower, "Tower/Tower", 3.4f, 2.5f, tower, true, new Color(0.35f, 0.18f, 0.22f),
                template =>
                {
                    TowerShooter.Decorate(template);
                    template.AddComponent<TowerShooter>();
                });
            Register(PoolKeys.Coin, CreateCoinTemplate());
            Register(PoolKeys.Fireball, TowerProjectile.CreateTemplate());
            Register(PoolKeys.Treant, CreateTreantTemplate());
            Register(PoolKeys.Wings, CreateWingsPickupTemplate());

            // Objects that first appear mid-run are created up front to avoid a hitch.
            pools[PoolKeys.Treant].Prewarm(4);
            pools[PoolKeys.Fireball].Prewarm(6);
            pools[PoolKeys.Wings].Prewarm(1);
        }

        public static string ForestCardKey(int variant) => PoolKeys.ForestCard + " " + variant;
        public static string ForestWallKey(int variant) => "Forest Wall " + variant;

        public ObjectPool Pool(string key) => pools[key];

        public GameObject Get(string key, Transform parent, Vector3 localPosition, float yaw = 0f) =>
            pools[key].Get(parent, localPosition, Quaternion.Euler(0f, yaw, 0f));

        public IEnumerable<ObjectPool> AllPools => pools.Values;

        private void Register(string key, GameObject template)
        {
            template.name = key + " Template";
            template.transform.SetParent(templateRoot, false);
            template.SetActive(false);
            pools[key] = new ObjectPool(key, () => UnityEngine.Object.Instantiate(template), storage);
        }

        private void Register(string key, Func<GameObject> buildTemplate) => Register(key, buildTemplate());

        private void RegisterModel(string key, string resourcePath, float height, float minimumWidth, Material material,
            bool obstacle, Color fallbackColor, Action<GameObject> configure = null, bool yUp = false)
        {
            GameObject template = BuildNormalizedModel(key, resourcePath, height, minimumWidth, material, fallbackColor, yUp);
            if (obstacle) AddHitbox(template);
            configure?.Invoke(template);
            Register(key, template);
        }

        /// <summary>Wraps a model so its root sits on the ground at the bottom center with the given height.</summary>
        private GameObject BuildNormalizedModel(string key, string resourcePath, float height, float minimumWidth,
            Material material, Color fallbackColor, bool yUp = false)
        {
            var root = new GameObject(key);
            root.transform.SetParent(templateRoot, false);
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            GameObject model;
            if (prefab != null)
            {
                model = UnityEngine.Object.Instantiate(prefab, root.transform);
                // FBX exports here are Z-up; glTF imports are already Y-up.
                model.transform.localRotation = yUp ? Quaternion.identity : Quaternion.Euler(-90f, 0f, 0f);
            }
            else
            {
                Debug.LogWarning($"Model '{resourcePath}' not found in Resources, using a placeholder cube.");
                model = GameObject.CreatePrimitive(PrimitiveType.Cube);
                model.transform.SetParent(root.transform, false);
                model.GetComponent<Renderer>().sharedMaterial = ColorMaterial(fallbackColor);
            }
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (material != null) renderer.sharedMaterial = material;
                // Some exported trees carry a helper cube used as a collision proxy.
                if (renderer.gameObject != model && renderer.gameObject.name.Equals("Cube", StringComparison.OrdinalIgnoreCase))
                    renderer.enabled = false;
            }
            foreach (Collider collider in model.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(collider);

            if (renderers.Length > 0)
            {
                Bounds bounds = GetBounds(renderers);
                if (bounds.size.y > 0.001f) model.transform.localScale *= height / bounds.size.y;
                bounds = GetBounds(renderers);
                if (minimumWidth > 0f && bounds.size.x < minimumWidth && bounds.size.x > 0.001f)
                    model.transform.localScale = Vector3.Scale(model.transform.localScale,
                        new Vector3(minimumWidth / bounds.size.x, 1f, 1f));
                bounds = GetBounds(renderers);
                Vector3 local = root.transform.InverseTransformPoint(bounds.center);
                model.transform.localPosition -= new Vector3(local.x, local.y - bounds.extents.y, local.z);
            }
            return root;
        }

        private static void AddHitbox(GameObject template)
        {
            Renderer[] renderers = template.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = GetBounds(renderers);
            var hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(template.transform, false);
            hitbox.transform.position = bounds.center;
            BoxCollider collider = hitbox.AddComponent<BoxCollider>();
            collider.size = new Vector3(bounds.size.x, bounds.size.y, Mathf.Min(bounds.size.z, 1.1f));
            collider.isTrigger = true;
            hitbox.AddComponent<ObstacleTrigger>();
        }

        private GameObject CreateForestCard(int variant)
        {
            var pivot = new GameObject(ForestCardKey(variant));
            pivot.transform.SetParent(templateRoot, false);
            Material material = Resources.Load<Material>($"Environment/PointForest1{variant + 1}");
            Texture texture = material != null ? material.GetTexture("_BaseMap") : null;
            float aspect = texture != null ? (float)texture.height / texture.width : 0.81f;

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Image";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(pivot.transform, false);
            const float width = 8f;
            quad.transform.localScale = new Vector3(width, width * aspect, 1f);
            quad.transform.localPosition = new Vector3(0f, width * aspect * 0.5f, 0f);
            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material != null ? material : ColorMaterial(new Color(0.15f, 0.3f, 0.15f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return pivot;
        }

        /// <summary>
        /// A wide painted strip of forest (ForestSide images) standing behind the side trees and facing
        /// the road, so the forest reads as dense and continuous.
        /// </summary>
        private GameObject CreateForestWall(int variant)
        {
            var root = new GameObject(ForestWallKey(variant));
            root.transform.SetParent(templateRoot, false);
            Material material = Resources.Load<Material>("Environment/" + ForestWallMaterials[variant]);
            Texture texture = material != null ? material.GetTexture("_BaseMap") : null;
            float aspect = texture != null ? (float)texture.height / texture.width : 0.42f;

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Image";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, false);
            const float width = 30f;
            quad.transform.localScale = new Vector3(width, width * aspect, 1f);
            // Slightly sunk so the painted undergrowth meets the grass.
            quad.transform.localPosition = new Vector3(0f, width * aspect * 0.5f - 0.6f, 0f);
            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material != null ? material : ColorMaterial(new Color(0.12f, 0.28f, 0.14f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return root;
        }

        /// <summary>A tree trunk lying across the lane, about knee-to-hip high, with sawn ends and branch stubs.</summary>
        private GameObject CreateFallenLog(Material bark)
        {
            var root = new GameObject("Fallen Log");
            root.transform.SetParent(templateRoot, false);
            Material cut = ColorMaterial(new Color(0.78f, 0.62f, 0.4f));
            Material barkTiled = new Material(bark) { name = "Log Bark" };
            barkTiled.SetTextureScale("_BaseMap", new Vector2(3f, 1f));
            const float radius = 0.5f, length = 2.7f;
            // Trunk: a cylinder rotated to lie along X.
            AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(0f, radius, 0f), new Vector3(radius * 2f, length * 0.5f, radius * 2f),
                new Vector3(0f, 0f, 90f), barkTiled);
            AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(-length * 0.5f, radius, 0f), new Vector3(radius * 1.9f, 0.02f, radius * 1.9f),
                new Vector3(0f, 0f, 90f), cut);
            AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(length * 0.5f, radius, 0f), new Vector3(radius * 1.9f, 0.02f, radius * 1.9f),
                new Vector3(0f, 0f, 90f), cut);
            AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(-0.5f, radius * 1.8f, 0.05f), new Vector3(0.18f, 0.35f, 0.18f),
                new Vector3(20f, 0f, -25f), barkTiled);
            AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(0.7f, radius * 1.6f, -0.1f), new Vector3(0.14f, 0.28f, 0.14f),
                new Vector3(-30f, 0f, 35f), barkTiled);
            AddHitbox(root);
            return root;
        }

        private GameObject CreateCoinTemplate()
        {
            var root = new GameObject(PoolKeys.Coin);
            root.transform.SetParent(templateRoot, false);
            // Faces point along ±Y in the mesh; tip the coin upright so it spins like a wheel.
            var coin = new GameObject("Coin");
            coin.transform.SetParent(root.transform, false);
            coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            coin.transform.localScale = Vector3.one * 1.25f;
            Material gold = CoinMaterial(new Color(1f, 0.76f, 0.18f), new Color(0.35f, 0.2f, 0.02f));
            Material emblem = CoinMaterial(new Color(1f, 0.88f, 0.45f), new Color(0.55f, 0.36f, 0.05f));
            AddMesh(coin.transform, "Body", CoinMesh.Body, gold);
            AddMesh(coin.transform, "Star", CoinMesh.Star, emblem);
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.45f;
            root.AddComponent<CoinPickup>();
            return root;
        }

        /// <summary>Polished gold: metallic, glossy and faintly self-lit so coins read through the fog.</summary>
        private static Material CoinMaterial(Color baseColor, Color emission)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", 0.85f);
            material.SetFloat("_Smoothness", 0.78f);
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
            return material;
        }

        private static void AddMesh(Transform parent, string name, Mesh mesh, Material material)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>
        /// Treant: uses Resources/Heroes/Treant if an artist adds it, otherwise a primitive stand-in
        /// (trunk, crown, arms and glowing eyes facing the courier).
        /// </summary>
        private GameObject CreateTreantTemplate()
        {
            var root = new GameObject(PoolKeys.Treant);
            root.transform.SetParent(templateRoot, false);
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);

            if (Resources.Load<GameObject>("Heroes/Treant") != null)
            {
                GameObject model = BuildNormalizedModel("Treant Model", "Heroes/Treant", Treant.Height, 0f, null, Color.gray);
                model.transform.SetParent(body.transform, false);
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else
            {
                Material bark = ColorMaterial(new Color(0.33f, 0.22f, 0.13f));
                Material leaves = ColorMaterial(new Color(0.18f, 0.42f, 0.16f));
                Material moss = ColorMaterial(new Color(0.27f, 0.36f, 0.14f));
                var eyes = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                eyes.SetColor("_BaseColor", new Color(1f, 0.9f, 0.3f));
                eyes.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.2f) * 3f);
                eyes.EnableKeyword("_EMISSION");

                AddPart(body.transform, PrimitiveType.Cube, new Vector3(-0.4f, 0.45f, 0f), new Vector3(0.55f, 0.9f, 0.55f), Vector3.zero, bark);
                AddPart(body.transform, PrimitiveType.Cube, new Vector3(0.4f, 0.45f, 0f), new Vector3(0.55f, 0.9f, 0.55f), Vector3.zero, bark);
                AddPart(body.transform, PrimitiveType.Cube, new Vector3(0f, 1.75f, 0f), new Vector3(1.4f, 1.9f, 1f), Vector3.zero, bark);
                AddPart(body.transform, PrimitiveType.Cube, new Vector3(-1f, 2f, 0f), new Vector3(0.38f, 1.7f, 0.38f), new Vector3(0f, 0f, -28f), bark);
                AddPart(body.transform, PrimitiveType.Cube, new Vector3(1f, 2f, 0f), new Vector3(0.38f, 1.7f, 0.38f), new Vector3(0f, 0f, 28f), bark);
                AddPart(body.transform, PrimitiveType.Sphere, new Vector3(0f, 3.05f, 0.1f), new Vector3(2.5f, 1.5f, 1.9f), Vector3.zero, leaves);
                AddPart(body.transform, PrimitiveType.Sphere, new Vector3(-0.8f, 2.75f, 0f), new Vector3(1.2f, 0.9f, 1.1f), Vector3.zero, moss);
                AddPart(body.transform, PrimitiveType.Sphere, new Vector3(0.85f, 2.8f, 0f), new Vector3(1.1f, 0.85f, 1f), Vector3.zero, moss);
                AddPart(body.transform, PrimitiveType.Sphere, new Vector3(-0.3f, 2.25f, -0.52f), Vector3.one * 0.22f, Vector3.zero, eyes);
                AddPart(body.transform, PrimitiveType.Sphere, new Vector3(0.3f, 2.25f, -0.52f), Vector3.one * 0.22f, Vector3.zero, eyes);
            }

            BoxCollider hitbox = root.AddComponent<BoxCollider>();
            hitbox.isTrigger = true;
            hitbox.size = new Vector3(2.3f, Treant.Height, 1.4f);
            hitbox.center = new Vector3(0f, Treant.Height * 0.5f, 0f);
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            CreateDust(root.transform);
            root.AddComponent<Treant>();
            return root;
        }

        /// <summary>
        /// Crystal Maiden's icy wings: two fans of glowing feathers on pivots that <see cref="WingFlap"/> rotates.
        /// The root's origin is between the shoulder blades; the wings spread along ±X.
        /// </summary>
        public GameObject CreateWingsVisual(Transform parent)
        {
            var root = new GameObject("Wings");
            root.transform.SetParent(parent, false);
            var feather = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            feather.SetColor("_BaseColor", new Color(0.86f, 0.95f, 1f));
            feather.SetColor("_EmissionColor", new Color(0.35f, 0.75f, 1f) * 1.6f);
            feather.EnableKeyword("_EMISSION");
            feather.SetFloat("_Smoothness", 0.8f);
            var tip = new Material(feather);
            tip.SetColor("_BaseColor", new Color(0.55f, 0.82f, 1f));

            Transform left = BuildWing(root.transform, -1f, feather, tip);
            Transform right = BuildWing(root.transform, 1f, feather, tip);
            WingFlap flap = root.AddComponent<WingFlap>();
            flap.left = left;
            flap.right = right;
            return root;
        }

        private static Transform BuildWing(Transform root, float side, Material feather, Material tip)
        {
            var pivot = new GameObject(side < 0f ? "Left Wing" : "Right Wing").transform;
            pivot.SetParent(root, false);
            // Three layers fanned out from the shoulder like a bird's wing: long primaries at the outer
            // edge, shorter secondaries closer to the body and small coverts along the top.
            AddFeatherRow(pivot, side, 7, new Vector2(0.35f, 0.42f), new Vector2(1.05f, 0.18f), 1.15f, 0.7f, 15f, -60f, 0.16f, tip, 0.02f);
            AddFeatherRow(pivot, side, 6, new Vector2(0.18f, 0.36f), new Vector2(0.75f, 0.26f), 0.75f, 0.5f, -10f, -75f, 0.15f, feather, 0.05f);
            AddFeatherRow(pivot, side, 5, new Vector2(0.12f, 0.46f), new Vector2(0.95f, 0.38f), 0.42f, 0.32f, 8f, -25f, 0.14f, feather, 0.08f);
            foreach (Renderer renderer in pivot.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            return pivot;
        }

        /// <summary>
        /// One row of feathers: each is a long flattened ellipsoid anchored at a point on the line from
        /// <paramref name="start"/> to <paramref name="end"/>, pointing outward at an angle that fans from
        /// <paramref name="angleFrom"/> to <paramref name="angleTo"/> degrees.
        /// </summary>
        private static void AddFeatherRow(Transform pivot, float side, int count, Vector2 start, Vector2 end,
            float lengthFrom, float lengthTo, float angleFrom, float angleTo, float width, Material material, float depth)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (count - 1f);
                Vector2 anchor = Vector2.Lerp(start, end, t);
                float length = Mathf.Lerp(lengthFrom, lengthTo, t);
                float angle = Mathf.Lerp(angleFrom, angleTo, t);
                Vector2 direction = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 center = anchor + direction * (length * 0.5f);
                AddPart(pivot, PrimitiveType.Sphere, new Vector3(side * center.x, center.y, depth),
                    new Vector3(length, width, 0.05f), new Vector3(0f, 0f, side * angle), material);
            }
        }

        private GameObject CreateWingsPickupTemplate()
        {
            var root = new GameObject(PoolKeys.Wings);
            root.transform.SetParent(templateRoot, false);
            GameObject wings = CreateWingsVisual(root.transform);
            wings.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            wings.transform.localScale = Vector3.one * 1.1f;
            wings.GetComponent<WingFlap>().flapsPerSecond = 1.1f;

            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(0.45f, 0.8f, 1f);
            glow.intensity = 2f;
            glow.range = 5f;

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;
            root.AddComponent<WingsPickup>();
            return root;
        }

        private static void AddPart(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 euler, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>Clouds of earth kicked up by the treant's steps.</summary>
        private static void CreateDust(Transform parent)
        {
            var dustObject = new GameObject("Dust");
            dustObject.transform.SetParent(parent, false);
            dustObject.transform.localPosition = new Vector3(0f, 0.15f, 0.3f);
            ParticleSystem dust = dustObject.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = dust.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.9f;
            main.startSpeed = 1.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startColor = new Color(0.45f, 0.36f, 0.25f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            ParticleSystem.EmissionModule emission = dust.emission;
            emission.rateOverTime = 28f;
            ParticleSystem.ShapeModule shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.6f, 0.1f, 0.6f);
            ParticleSystem.ColorOverLifetimeModule fade = dust.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            ParticleSystemRenderer renderer = dustObject.GetComponent<ParticleSystemRenderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetColor("_BaseColor", new Color(0.55f, 0.45f, 0.32f, 0.6f));
            renderer.sharedMaterial = material;
        }

        /// <summary>The courier's visual model, or a capsule if the prefab is missing.</summary>
        public GameObject CreateCourierVisual(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>("CourierVisual");
            if (prefab != null)
            {
                GameObject visual = UnityEngine.Object.Instantiate(prefab, parent);
                visual.name = "Courier Model";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                return visual;
            }
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Temporary Courier";
            UnityEngine.Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.transform.SetParent(parent, false);
            capsule.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            capsule.GetComponent<Renderer>().sharedMaterial = ColorMaterial(new Color(0.95f, 0.65f, 0.18f));
            return capsule;
        }

        /// <summary>Crystal Maiden standing at the delivery point, facing the courier.</summary>
        public GameObject CreateCrystalMaiden(Transform parent, Vector3 localPosition)
        {
            var root = new GameObject("Crystal Maiden");
            root.transform.SetParent(parent, false);
            GameObject prefab = Resources.Load<GameObject>("Heroes/CrystalMaiden");
            if (prefab == null)
            {
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                UnityEngine.Object.DestroyImmediate(placeholder.GetComponent<Collider>());
                placeholder.transform.SetParent(root.transform, false);
                placeholder.transform.localScale = new Vector3(1f, 1.35f, 1f);
                placeholder.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                placeholder.GetComponent<Renderer>().sharedMaterial = ColorMaterial(new Color(0.45f, 0.75f, 1f));
                root.transform.localPosition = localPosition;
                return root;
            }

            GameObject maiden = UnityEngine.Object.Instantiate(prefab, root.transform);
            maiden.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Renderer[] renderers = maiden.GetComponentsInChildren<Renderer>();
            const string textures = "Heroes/CrystalMaidenTextures/";
            Material body = CharacterMaterial(textures + "crystal_maiden_color_psd_764d83fe");
            Material cape = CharacterMaterial(textures + "crystal_maiden_cape_color_psd_a138152d");
            Material cuffs = CharacterMaterial(textures + "crystal_maiden_cuffs_color_psd_b96ac1c");
            Material shoulders = CharacterMaterial(textures + "crystal_maiden_shoulder_color_psd_592a78cc");
            Material staff = CharacterMaterial(textures + "crystal_maiden_staff_color_psd_1b36259a");
            Material head = CharacterMaterial(textures + "head_item_color_tga_71ffe59e");
            foreach (Renderer renderer in renderers)
            {
                string name = renderer.gameObject.name.ToLowerInvariant();
                renderer.sharedMaterial = name.Contains("cape") ? cape : name.Contains("cuffs") ? cuffs :
                    name.Contains("shoulder") ? shoulders : name.Contains("staff") ? staff :
                    name.Contains("head_item") ? head : body;
            }
            foreach (Collider collider in maiden.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(collider);

            if (renderers.Length > 0)
            {
                Bounds bounds = GetBounds(renderers);
                if (bounds.size.y > 0.001f) maiden.transform.localScale *= 2.7f / bounds.size.y;
                bounds = GetBounds(renderers);
                Vector3 local = root.transform.InverseTransformPoint(bounds.center);
                maiden.transform.localPosition -= new Vector3(local.x, local.y - bounds.extents.y, local.z);
            }
            root.transform.localPosition = localPosition;

            Animator animator = maiden.GetComponentInChildren<Animator>();
            if (animator == null) animator = maiden.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Heroes/CrystalMaidenIdle");
            animator.applyRootMotion = false;
            root.AddComponent<RecipientAnimationLoop>().animator = animator;
            maiden.AddComponent<CrystalMaidenPose>();
            return root;
        }

        private Material CharacterMaterial(string texturePath)
        {
            Material material = TextureMaterial(texturePath, true, 0.2f);
            material.SetFloat("_Cull", 0f);
            return material;
        }

        public Material TextureMaterial(string resourcePath, bool alphaClip, float smoothness)
        {
            if (materials.TryGetValue(resourcePath, out Material cached)) return cached;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>(resourcePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            if (alphaClip)
            {
                material.SetFloat("_Cull", 0f);
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", 0.35f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = 2450;
            }
            materials[resourcePath] = material;
            return material;
        }

        public Material ColorMaterial(Color color)
        {
            string key = "color:" + ColorUtility.ToHtmlStringRGBA(color);
            if (materials.TryGetValue(key, out Material cached)) return cached;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            materials[key] = material;
            return material;
        }

        private static Bounds GetBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
