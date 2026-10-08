using UnityEngine;

namespace CourierRunner
{
    /// <summary>Builds the hand-placed prototype level. Replaced by segment generation in stage 1.</summary>
    public sealed class PrototypeWorld : MonoBehaviour
    {
        private static readonly Color RoadColor = new(0.16f, 0.19f, 0.25f);
        private Material oakMaterial;
        private Material pineMaterial;
        private Material stumpMaterial;
        private Material wallMaterial;
        private Material towerMaterial;
        private Material rockMaterial;
        private Material wellMaterial;

        public void Build()
        {
            Application.targetFrameRate = 60;

            BuildLighting();
            BuildRoad();
            BuildScenery();
            RunnerController runner = BuildRunner();
            BuildCamera(runner.transform);
            BuildLevel(runner);
            gameObject.AddComponent<GameFlowUI>().runner = runner;
        }

        private void BuildLighting()
        {
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.6f);
            Light existing = Object.FindAnyObjectByType<Light>();
            if (existing != null)
                return;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private void BuildRoad()
        {
            GameObject grass = CreatePrimitive("Forest Grass", PrimitiveType.Cube, new Vector3(0f, -0.34f, 90f),
                new Vector3(48f, 0.55f, 200f), new Color(0.16f, 0.27f, 0.095f), true);
            Material grassMaterial = Resources.Load<Material>("Environment/Grass");
            if (grassMaterial != null) grass.GetComponent<Renderer>().sharedMaterial = grassMaterial;
            else grass.GetComponent<Renderer>().material.SetFloat("_Smoothness", 0.02f);

            GameObject road = CreatePrimitive("Stone Forest Path", PrimitiveType.Cube, new Vector3(0f, -0.255f, 90f),
                new Vector3(10.2f, 0.5f, 200f), Color.white, true);
            Material pathMaterial = Resources.Load<Material>("Environment/Path");
            if (pathMaterial != null) road.GetComponent<Renderer>().sharedMaterial = pathMaterial;
        }

        private void BuildScenery()
        {
            GameObject oak = Resources.Load<GameObject>("Trees/TreeOak");
            GameObject pine = Resources.Load<GameObject>("Trees/TreePine");
            GameObject stump = Resources.Load<GameObject>("Trees/TreeStump");
            GameObject singleRock = Resources.Load<GameObject>("Environment/RockSingle");
            GameObject well = Resources.Load<GameObject>("Decorations/Well");
            GameObject[] trees = { oak, pine };
            oakMaterial = CreateTreeMaterial("Trees/Textures/tree_oak_leaves_00_color_psd_c6c1c88b");
            pineMaterial = CreateTreeMaterial("Trees/Textures/tree_pine_frond_00_color_psd_cad45fc8");
            stumpMaterial = CreateTreeMaterial("Trees/Textures/tree_stump001_color_psd_69850232");
            rockMaterial = CreateEnvironmentMaterial("Decorations/rock_color", false, 0.16f);
            wellMaterial = CreateEnvironmentMaterial("Decorations/well_color", false, 0.18f);
            wallMaterial = CreateEnvironmentMaterial("Walls/wall_color", false, 0.14f);
            towerMaterial = CreateEnvironmentMaterial("Tower/tower_color", false, 0.2f);

            Random.State previousState = Random.state;
            Random.InitState(7251);
            for (int i = 0; i < 150; i++)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                float distance = Random.Range(7f, 18f);
                float z = Random.Range(-4f, 190f);
                PlaceTree(trees[Random.Range(0, trees.Length)], side * distance, z);
            }

            GameObject[] rocks = { singleRock };
            for (int i = 0; i < 34; i++)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                PlaceDecoration(rocks[Random.Range(0, rocks.Length)], side * Random.Range(6.5f, 15f),
                    Random.Range(0f, 185f), Random.Range(0.65f, 1.4f), null);
            }

            for (int i = 0; i < 8; i++)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                PlaceDecoration(stump, side * Random.Range(6.5f, 12f), Random.Range(0f, 185f), 1.25f, stumpMaterial);
            }

            PlaceDecoration(well, -8.2f, 48f, 2.2f, wellMaterial);
            PlaceDecoration(well, 9.5f, 126f, 2.2f, wellMaterial);
            Random.state = previousState;
        }

        private Material CreateTreeMaterial(string resourcePath)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>(resourcePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.12f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.35f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = 2450;
            return material;
        }

        private Material CreateEnvironmentMaterial(string resourcePath, bool alphaClip, float smoothness)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>(resourcePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            if (alphaClip)
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", 0.35f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = 2450;
            }
            return material;
        }

        private void PlaceTree(GameObject prefab, float x, float z)
        {
            if (prefab == null) return;
            GameObject tree = Object.Instantiate(prefab, transform);
            tree.name = prefab.name;
            tree.transform.localPosition = Vector3.zero;
            tree.transform.localRotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
            float variation = Random.Range(0.82f, 1.18f);
            Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                tree.transform.localPosition = new Vector3(x, 0f, z);
                return;
            }

            Material treeMaterial = prefab.name.Contains("Pine") ? pineMaterial :
                prefab.name.Contains("Stump") ? stumpMaterial : oakMaterial;
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterial = treeMaterial;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float targetHeight = prefab.name.Contains("Pine") ? 6.2f : prefab.name.Contains("Stump") ? 1.25f : 5.5f;
            if (bounds.size.y > 0.001f)
                tree.transform.localScale *= targetHeight / bounds.size.y * variation;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            tree.transform.position += new Vector3(x - bounds.center.x, -bounds.min.y, z - bounds.center.z);

            foreach (Collider collider in tree.GetComponentsInChildren<Collider>())
                DestroyGeneratedObject(collider);

            foreach (Renderer renderer in renderers)
                if (renderer.gameObject.name.Equals("Cube", System.StringComparison.OrdinalIgnoreCase))
                    renderer.enabled = false;
        }

        private GameObject PlaceDecoration(GameObject prefab, float x, float z, float targetHeight, Material material)
        {
            if (prefab == null) return null;
            GameObject item = Object.Instantiate(prefab, transform);
            item.name = prefab.name;
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return item;
            if (material != null)
                foreach (Renderer renderer in renderers) renderer.sharedMaterial = material;

            Bounds bounds = GetBounds(renderers);
            if (bounds.size.y > 0.001f) item.transform.localScale *= targetHeight / bounds.size.y;
            bounds = GetBounds(renderers);
            float side = Mathf.Sign(x == 0f ? 1f : x);
            float safeX = 5.15f + bounds.extents.x + 0.2f;
            if (Mathf.Abs(x) < safeX) x = side * safeX;
            item.transform.position += new Vector3(x - bounds.center.x, -bounds.min.y, z - bounds.center.z);
            foreach (Collider collider in item.GetComponentsInChildren<Collider>()) DestroyGeneratedObject(collider);
            return item;
        }

        private static Bounds GetBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private RunnerController BuildRunner()
        {
            var player = new GameObject("Courier");
            player.transform.position = new Vector3(0f, 1.05f, 0f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.48f;
            controller.center = Vector3.zero;

            var runner = player.AddComponent<RunnerController>();
            GameObject courierPrefab = Resources.Load<GameObject>("CourierVisual");
            if (courierPrefab != null)
            {
                GameObject visual = Object.Instantiate(courierPrefab, player.transform);
                visual.name = "Courier Model";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
            else
            {
                CreatePrimitive("Temporary Courier", PrimitiveType.Capsule, Vector3.zero,
                    new Vector3(0.8f, 1f, 0.8f), new Color(0.95f, 0.65f, 0.18f), false, player.transform);
            }
            return runner;
        }

        private void BuildCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.fieldOfView = 62f;
            var follow = camera.gameObject.AddComponent<RunnerCamera>();
            follow.target = target;
            camera.transform.position = target.position + new Vector3(0f, 5f, -8f);
        }

        private void BuildLevel(RunnerController runner)
        {
            AddWall(-3f, 20f, 1);
            AddRock(0f, 31f, 1);
            AddWall(3f, 42f, 2);
            AddTower(-3f, 57f, runner);

            AddWall(-3f, 72f, 3);
            AddRock(0f, 72f, 2);
            AddWall(3f, 72f, 1);

            AddTower(3f, 90f, runner);
            AddRock(-3f, 102f, 1);
            AddWall(0f, 102f, 2);

            AddWall(-3f, 119f, 1);
            AddRock(0f, 119f, 2);
            AddWall(3f, 119f, 3);

            AddTower(0f, 137f, runner);

            AddCoinLine(0, 7f, 15f, 4);
            AddCoinLine(1, 24f, 37f, 5);
            AddCoinLine(-1, 46f, 54f, 4);
            AddCoinLine(1, 59f, 67f, 4);
            AddJumpCoins(72f);
            AddCoinLine(-1, 79f, 87f, 4);
            AddCoinLine(1, 96f, 108f, 5);
            AddJumpCoins(119f);
            AddCoinLine(-1, 126f, 134f, 4);
            AddCoinLine(1, 140f, 148f, 4);

            BuildRecipient();

            GameObject finish = new GameObject("Delivery Finish");
            finish.transform.position = new Vector3(0f, 1f, 153f);
            var finishCollider = finish.AddComponent<BoxCollider>();
            finishCollider.size = new Vector3(11f, 3f, 1f);
            finishCollider.isTrigger = true;
            finish.AddComponent<FinishTrigger>().runner = runner;
        }

        private void BuildRecipient()
        {
            GameObject prefab = Resources.Load<GameObject>("Heroes/CrystalMaiden");
            if (prefab == null) return;
            GameObject maiden = Object.Instantiate(prefab, transform);
            maiden.name = "Crystal Maiden";
            maiden.transform.localPosition = Vector3.zero;
            maiden.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Renderer[] renderers = maiden.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Material body = CreateCharacterMaterial("Heroes/CrystalMaidenTextures/crystal_maiden_color_psd_764d83fe");
            Material cape = CreateCharacterMaterial("Heroes/CrystalMaidenTextures/crystal_maiden_cape_color_psd_a138152d");
            Material cuffs = CreateCharacterMaterial("Heroes/CrystalMaidenTextures/crystal_maiden_cuffs_color_psd_b96ac1c");
            Material shoulders = CreateCharacterMaterial("Heroes/CrystalMaidenTextures/crystal_maiden_shoulder_color_psd_592a78cc");
            Material staff = CreateCharacterMaterial("Heroes/CrystalMaidenTextures/crystal_maiden_staff_color_psd_1b36259a");
            Material head = CreateCharacterMaterial("Heroes/CrystalMaidenTextures/head_item_color_tga_71ffe59e");

            foreach (Renderer renderer in renderers)
            {
                string name = renderer.gameObject.name.ToLowerInvariant();
                renderer.sharedMaterial = name.Contains("cape") ? cape : name.Contains("cuffs") ? cuffs :
                    name.Contains("shoulder") ? shoulders : name.Contains("staff") ? staff :
                    name.Contains("head_item") ? head : body;
            }

            Bounds bounds = GetBounds(renderers);
            if (bounds.size.y > 0.001f) maiden.transform.localScale *= 2.7f / bounds.size.y;
            bounds = GetBounds(renderers);
            maiden.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, 158f - bounds.center.z);

            Animator animator = maiden.GetComponentInChildren<Animator>();
            if (animator == null) animator = maiden.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Heroes/CrystalMaidenIdle");
            animator.applyRootMotion = false;
            maiden.AddComponent<RecipientAnimationLoop>().animator = animator;
            foreach (Collider collider in maiden.GetComponentsInChildren<Collider>()) DestroyGeneratedObject(collider);
        }

        private Material CreateCharacterMaterial(string texturePath)
        {
            Material material = CreateEnvironmentMaterial(texturePath, true, 0.2f);
            material.SetFloat("_Cull", 0f);
            return material;
        }

        private void AddWall(float x, float z, int variant)
        {
            GameObject prefab = Resources.Load<GameObject>($"Walls/Wall{variant}");
            GameObject obstacle = PlaceGameplayModel(prefab, x, z, 1.9f, 2.75f, wallMaterial);
            if (obstacle == null) return;
            obstacle.name = $"Wood Wall {variant} - Jump";
            AddObstacleCollider(obstacle);
        }

        private void AddRock(float x, float z, int variant)
        {
            GameObject prefab = Resources.Load<GameObject>("Environment/RockSingle");
            GameObject obstacle = PlaceGameplayModel(prefab, x, z, 1.15f, 2.1f, null);
            if (obstacle == null) return;
            obstacle.name = $"Rock {variant} - Jump";
            AddObstacleCollider(obstacle);
        }

        private void AddTower(float x, float z, RunnerController runner)
        {
            GameObject prefab = Resources.Load<GameObject>("Tower/Tower");
            GameObject tower = PlaceGameplayModel(prefab, x, z, 3.4f, 2.5f, towerMaterial);
            if (tower == null) return;
            tower.name = "Lane Tower";
            AddObstacleCollider(tower);
            TowerShooter shooter = tower.AddComponent<TowerShooter>();
            shooter.runner = runner;
            shooter.laneX = x;
            shooter.towerZ = z;
        }

        private GameObject PlaceGameplayModel(GameObject prefab, float x, float z, float targetHeight,
            float minimumWidth, Material material)
        {
            if (prefab == null) return null;
            GameObject item = Object.Instantiate(prefab, transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return item;
            if (material != null)
                foreach (Renderer renderer in renderers) renderer.sharedMaterial = material;

            Bounds bounds = GetBounds(renderers);
            if (bounds.size.y > 0.001f) item.transform.localScale *= targetHeight / bounds.size.y;
            bounds = GetBounds(renderers);
            if (bounds.size.x < minimumWidth && bounds.size.x > 0.001f)
                item.transform.localScale = Vector3.Scale(item.transform.localScale,
                    new Vector3(minimumWidth / bounds.size.x, 1f, 1f));
            bounds = GetBounds(renderers);
            item.transform.position += new Vector3(x - bounds.center.x, -bounds.min.y, z - bounds.center.z);
            foreach (Collider collider in item.GetComponentsInChildren<Collider>()) DestroyGeneratedObject(collider);
            return item;
        }

        private void AddCoinLine(int lane, float startZ, float endZ, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float z = count == 1 ? startZ : Mathf.Lerp(startZ, endZ, i / (count - 1f));
                AddCoin(lane * 3f, 1f, z);
            }
        }

        private void AddJumpCoins(float z)
        {
            AddCoin(-3f, 2.65f, z);
            AddCoin(0f, 2.65f, z);
            AddCoin(3f, 2.65f, z);
        }

        private void AddCoin(float x, float y, float z)
        {
            GameObject coin = CreatePrimitive("Gold", PrimitiveType.Cylinder,
                new Vector3(x, y, z), new Vector3(0.6f, 0.12f, 0.6f),
                new Color(1f, 0.75f, 0.05f), false);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            coin.GetComponent<Collider>().isTrigger = true;
            coin.AddComponent<CoinPickup>();
        }

        private void AddObstacleCollider(GameObject obstacle)
        {
            Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = GetBounds(renderers);
            GameObject hitbox = new GameObject(obstacle.name + " Hitbox");
            hitbox.transform.SetParent(transform, false);
            hitbox.transform.position = bounds.center;
            BoxCollider collider = hitbox.AddComponent<BoxCollider>();
            collider.size = new Vector3(bounds.size.x, bounds.size.y, Mathf.Min(bounds.size.z, 1.1f));
            collider.isTrigger = true;
            hitbox.AddComponent<ObstacleTrigger>();
        }

        private GameObject CreatePrimitive(string objectName, PrimitiveType type, Vector3 position,
            Vector3 scale, Color color, bool keepCollider, Transform parent = null)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = objectName;
            instance.transform.SetParent(parent == null ? transform : parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            instance.GetComponent<Renderer>().material = material;
            if (!keepCollider && instance.GetComponent<Collider>() != null &&
                !objectName.Contains("Gold") && !objectName.Contains("Jump") && !objectName.Contains("Tower"))
                DestroyGeneratedObject(instance.GetComponent<Collider>());
            return instance;
        }

        private static void DestroyGeneratedObject(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }
    }
}
