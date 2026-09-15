using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CourierRunner
{
    public static class PrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CreatePrototype();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CreatePrototype();

        private static void CreatePrototype()
        {
            EditableLevelRuntime editableLevel = Object.FindFirstObjectByType<EditableLevelRuntime>();
            if (editableLevel != null)
            {
                editableLevel.InitializeNow();
                return;
            }
            if (Object.FindFirstObjectByType<RunnerController>() != null)
                return;

            var world = new GameObject("Courier Runner Prototype");
            var builder = world.AddComponent<PrototypeWorld>();
            builder.Build();
        }
    }

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
            Light existing = Object.FindFirstObjectByType<Light>();
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

    public sealed class RunnerController : MonoBehaviour
    {
        public float forwardSpeed = 9f;
        public float laneWidth = 3f;
        public float laneChangeSpeed = 12f;
        public float jumpHeight = 2.15f;

        public int Coins { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasStarted { get; private set; }
        public bool HasDelivered { get; private set; }

        private CharacterController controller;
        private int lane;
        private float verticalSpeed;
        private Vector2 touchStart;
        private bool trackingTouch;
        private Animator animator;

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
                animator.applyRootMotion = false;
        }

        private void Update()
        {
            KeepLoopingAnimation();

            if (!IsRunning)
            {
                return;
            }

            ReadKeyboard();
            ReadTouch();
            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -1f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;

            float targetX = lane * laneWidth;
            float horizontal = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime)
                               - transform.position.x;
            controller.Move(new Vector3(horizontal, verticalSpeed * Time.deltaTime, forwardSpeed * Time.deltaTime));
        }

        private void KeepLoopingAnimation()
        {
            // A model can briefly exist without its controller while Unity refreshes imported
            // assets. Do not query its state in that interval: Unity otherwise logs a warning
            // every frame and the Console quickly becomes unusable.
            if (animator == null || !animator.isActiveAndEnabled ||
                animator.runtimeAnimatorController == null || animator.layerCount == 0)
                return;
            if (animator.IsInTransition(0)) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime < 1f) return;
            if (state.IsName("Run") || state.IsName("Idle"))
                animator.Play(state.shortNameHash, 0, 0f);
        }

        private void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) ChangeLane(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) ChangeLane(1);
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Jump();
        }

        private void ReadTouch()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return;
            var touch = screen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                touchStart = touch.position.ReadValue();
                trackingTouch = true;
            }
            if (!trackingTouch || !touch.press.wasReleasedThisFrame) return;
            Vector2 swipe = touch.position.ReadValue() - touchStart;
            trackingTouch = false;
            if (swipe.magnitude < 40f) return;
            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y)) ChangeLane(swipe.x > 0f ? 1 : -1);
            else if (swipe.y > 0f) Jump();
            // A downward swipe is intentionally unused: the game has no slide action.
        }

        private void ChangeLane(int direction) => lane = Mathf.Clamp(lane + direction, -1, 1);

        private void Jump()
        {
            if (controller.isGrounded)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        }

        public void BeginRun()
        {
            HasStarted = true;
            IsRunning = true;
        }

        public void MoveLeft() { if (IsRunning) ChangeLane(-1); }
        public void MoveRight() { if (IsRunning) ChangeLane(1); }
        public void JumpFromUi() { if (IsRunning) Jump(); }

        public void AddCoin() => Coins++;
        public void Crash()
        {
            if (!IsRunning) return;
            IsRunning = false;
            if (animator != null) animator.SetTrigger("Crash");
        }

        public void Deliver()
        {
            if (!IsRunning) return;
            HasDelivered = true;
            IsRunning = false;
            if (animator != null) animator.SetBool("Finished", true);
        }
    }

    public sealed class ObstacleTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner == null) return;
            // A trigger is raised only when the character volume actually intersects the obstacle.
            // This also catches landing on top of it, which the old height check incorrectly allowed.
            runner.Crash();
        }
    }

    public sealed class TowerShooter : MonoBehaviour
    {
        public RunnerController runner;
        public float laneX;
        public float towerZ;
        public float detectionDistance = 20f;
        public float fireInterval = 1.25f;
        private float cooldown = 0.5f;

        private void Update()
        {
            if (runner == null || !runner.IsRunning) return;
            cooldown -= Time.deltaTime;
            float forwardDistance = towerZ - runner.transform.position.z;
            bool sameLane = Mathf.Abs(laneX - runner.transform.position.x) < 1.15f;
            if (!sameLane || forwardDistance <= 1.5f || forwardDistance > detectionDistance || cooldown > 0f)
                return;

            cooldown = fireInterval;
            GameObject shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shot.name = "Tower Fireball";
            shot.transform.position = new Vector3(laneX, 1.25f, towerZ - 1.3f);
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
            shot.AddComponent<TowerProjectile>();
        }
    }

    public sealed class TowerProjectile : MonoBehaviour
    {
        public float speed = 13f;
        public float maxDistance = 16f;
        private Vector3 startPosition;

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

    public sealed class CoinPickup : MonoBehaviour
    {
        private void Update() => transform.Rotate(0f, 160f * Time.deltaTime, 0f, Space.World);

        private void OnTriggerEnter(Collider other)
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner == null) return;
            runner.AddCoin();
            Destroy(gameObject);
        }
    }

    public sealed class FinishTrigger : MonoBehaviour
    {
        public RunnerController runner;

        private void Update()
        {
            // CharacterController movement is not driven by a Rigidbody, so trigger callbacks
            // can be missed after scene reconstruction. Crossing the finish plane is the
            // authoritative fallback.
            if (runner != null && runner.IsRunning && runner.transform.position.z >= transform.position.z)
                runner.Deliver();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<RunnerController>() == runner)
                runner.Deliver();
        }
    }

    public sealed class RunnerCamera : MonoBehaviour
    {
        public Transform target;
        private readonly Vector3 offset = new(0f, 5f, -8f);
        private Camera gameCamera;
        private int lastScreenWidth;
        private int lastScreenHeight;

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

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, target.position + offset, 8f * Time.deltaTime);
            transform.LookAt(target.position + new Vector3(0f, 1f, 4f));
        }
    }

    public sealed class RecipientAnimationLoop : MonoBehaviour
    {
        public Animator animator;

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null || animator.IsInTransition(0)) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime >= 1f)
                animator.Play(state.shortNameHash, 0, 0f);
        }
    }

    public sealed class MenuArtFloat : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 startPosition;

        private void Awake()
        {
            rect = (RectTransform)transform;
            startPosition = rect.anchoredPosition;
            rect.localScale = Vector3.one * 1.025f;
        }

        private void Update()
        {
            // A slow, subtle loop keeps the illustrated title feeling alive without making
            // buttons or text harder to read.
            rect.anchoredPosition = startPosition + Vector2.up * (Mathf.Sin(Time.unscaledTime * 1.15f) * 7f);
        }
    }

    public sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private bool hovered;
        private bool pressed;
        private float phase;

        private void Awake() => phase = Mathf.Abs(GetInstanceID() % 100) * 0.07f;

        private void Update()
        {
            float breathing = 1f + Mathf.Sin(Time.unscaledTime * 1.8f + phase) * 0.008f;
            float interactionScale = pressed ? 0.955f : hovered ? 1.035f : 1f;
            float target = breathing * interactionScale;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target,
                                                12f * Time.unscaledDeltaTime);
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
    }

    public sealed class GameFlowUI : MonoBehaviour
    {
        public RunnerController runner;
        private const string CompletionKey = "CourierRunner.CrystalMaiden.Completed";
        private const string LaunchMarkerName = "__CourierLevelLaunchMarker";

        private enum ScreenState { Menu, Map, Playing, Result }
        private ScreenState state;
        private Font font;
        private RectTransform phone;
        private Image screenBackground;
        private GameObject page;
        private GameObject gameplayHud;
        private Text coinText;

        private static readonly Color Navy = new(0.035f, 0.055f, 0.11f, 0.98f);
        private static readonly Color Ice = new(0.25f, 0.72f, 0.95f, 1f);
        private static readonly Color Violet = new(0.36f, 0.23f, 0.67f, 1f);

        private void Start()
        {
            Debug.Log("GameFlowUI started.");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();
            GameObject launchMarker = GameObject.Find(LaunchMarkerName);
            if (launchMarker != null)
            {
                Destroy(launchMarker);
                StartGameplay();
            }
            else
            {
                ShowMainMenu();
            }
        }

        private void Update()
        {
            if (state != ScreenState.Playing || runner == null) return;
            coinText.text = $"ЗОЛОТО  {runner.Coins}";
            if (runner.IsRunning || !runner.HasStarted) return;

            if (runner.HasDelivered)
            {
                PlayerPrefs.SetInt(CompletionKey, 1);
                PlayerPrefs.Save();
            }
            ShowResult(runner.HasDelivered);
        }

        private void BuildCanvas()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventObject = new("UI EventSystem");
                eventObject.AddComponent<EventSystem>();
                eventObject.AddComponent<InputSystemUIInputModule>();
            }

            GameObject canvasObject = new("Game UI");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            screenBackground = CreateImage("Screen Background", canvasObject.transform, new Color(0.008f, 0.012f, 0.025f, 1f));
            Stretch(screenBackground.rectTransform);

            GameObject phoneObject = new("9x16 Safe Area", typeof(RectTransform), typeof(AspectRatioFitter));
            phoneObject.transform.SetParent(canvasObject.transform, false);
            phone = phoneObject.GetComponent<RectTransform>();
            Stretch(phone);
            AspectRatioFitter fitter = phoneObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 9f / 16f;
        }

        private void ShowMainMenu()
        {
            state = ScreenState.Menu;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Main Menu", Color.black);

            Texture2D backgroundTexture = Resources.Load<Texture2D>("UI/MainMenuBackground");
            if (backgroundTexture != null)
            {
                Image background = CreateImage("Menu Background", page.transform, Color.white);
                background.sprite = Sprite.Create(backgroundTexture,
                    new Rect(0f, 0f, backgroundTexture.width, backgroundTexture.height),
                    new Vector2(0.5f, 0.5f), 100f);
                background.type = Image.Type.Simple;
                Stretch(background.rectTransform);
            }

            Texture2D titleTexture = Resources.Load<Texture2D>("UI/MainMenuTitle");
            if (titleTexture != null)
            {
                Image floatingTitle = CreateImage("Floating Title", page.transform, Color.white);
                floatingTitle.sprite = Sprite.Create(titleTexture,
                    new Rect(0f, 0f, titleTexture.width, titleTexture.height),
                    new Vector2(0.5f, 0.5f), 100f);
                floatingTitle.type = Image.Type.Simple;
                floatingTitle.preserveAspect = true;
                SetAnchors(floatingTitle.rectTransform, new Vector2(0.01f, 0.665f), new Vector2(0.99f, 0.94f));
                floatingTitle.gameObject.AddComponent<MenuArtFloat>();
            }

            Button play = CreateButton(page.transform, "▶   ИГРАТЬ", new Vector2(0.045f, 0.045f), new Vector2(0.72f, 0.155f), Ice, 42);
            ApplyMenuButtonArt(play, "UI/PlayButton");
            play.onClick.AddListener(ShowMap);
            // Normalized X and Y ranges are different in a 9:16 canvas. A wider X span is
            // required for the settings control to be physically square and match Play's height.
            Button settings = CreateButton(page.transform, string.Empty, new Vector2(0.765f, 0.045f), new Vector2(0.955f, 0.155f), Color.white, 40);
            ApplyMenuButtonArt(settings, "UI/SettingsButton");
            settings.onClick.AddListener(ShowControls);
        }

        private void ShowMap()
        {
            state = ScreenState.Map;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Level Map", Navy);
            CreateText(page.transform, "КАРТА ДОСТАВОК", 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.94f), Color.white);
            CreateText(page.transform, "Выбери героя, которому нужна доставка", 27, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.84f), new Color(0.7f, 0.82f, 0.95f));

            bool completed = PlayerPrefs.GetInt(CompletionKey, 0) == 1;
            string icon = completed ? "✓" : "❄";
            Color cardColor = completed ? new Color(0.18f, 0.62f, 0.42f) : Violet;
            Button level = CreateButton(page.transform, icon + "\nCRYSTAL MAIDEN\nУровень 1", new Vector2(0.18f, 0.42f), new Vector2(0.82f, 0.68f), cardColor, 36);
            level.onClick.AddListener(ReloadIntoLevel);
            CreateText(page.transform, completed ? "Доставка выполнена" : "Доступно", 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.41f), completed ? new Color(0.5f, 1f, 0.7f) : Ice);

            Button back = CreateButton(page.transform, "‹  НАЗАД", new Vector2(0.08f, 0.07f), new Vector2(0.42f, 0.14f), new Color(0.16f, 0.2f, 0.3f), 28);
            back.onClick.AddListener(ShowMainMenu);
        }

        private void ShowControls()
        {
            state = ScreenState.Menu;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Controls", Navy);
            CreateText(page.transform, "НАСТРОЙКИ", 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.93f), Color.white);
            CreateText(page.transform, "УПРАВЛЕНИЕ", 31, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.73f), new Vector2(0.85f, 0.79f), Ice);

            CreateControlRow(page.transform, "A   или   ←", "Левая полоса", 0.64f);
            CreateControlRow(page.transform, "D   или   →", "Правая полоса", 0.49f);
            CreateControlRow(page.transform, "W   или   ↑", "Прыжок", 0.34f);
            CreateText(page.transform, "НА ТЕЛЕФОНЕ", 27, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.27f), Ice);
            CreateText(page.transform, "Свайп влево / вправо — смена полосы\nСвайп вверх — прыжок", 25, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.13f), new Vector2(0.90f, 0.22f), Color.white);

            Button back = CreateButton(page.transform, "‹  НАЗАД", new Vector2(0.08f, 0.045f), new Vector2(0.42f, 0.105f), new Color(0.16f, 0.2f, 0.3f), 27);
            back.onClick.AddListener(ShowMainMenu);
        }

        private void CreateControlRow(Transform parent, string keys, string action, float bottom)
        {
            Image keyPlate = CreateImage(keys + " Keys", parent, new Color(0.12f, 0.16f, 0.25f));
            SetAnchors(keyPlate.rectTransform, new Vector2(0.12f, bottom), new Vector2(0.48f, bottom + 0.105f));
            CreateText(keyPlate.transform, keys, 31, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Color.white);
            CreateText(parent, action, 29, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.54f, bottom), new Vector2(0.91f, bottom + 0.105f), new Color(0.75f, 0.88f, 1f));
        }

        private void ReloadIntoLevel()
        {
            GameObject oldMarker = GameObject.Find(LaunchMarkerName);
            if (oldMarker != null) Destroy(oldMarker);
            GameObject marker = new(LaunchMarkerName);
            DontDestroyOnLoad(marker);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void StartGameplay()
        {
            state = ScreenState.Playing;
            ClearPage();
            screenBackground.gameObject.SetActive(false);
            gameplayHud = new GameObject("Gameplay HUD", typeof(RectTransform));
            gameplayHud.transform.SetParent(phone, false);
            Stretch(gameplayHud.GetComponent<RectTransform>());

            Image coinPlate = CreateImage("Coin Plate", gameplayHud.transform, new Color(0.03f, 0.05f, 0.1f, 0.72f));
            SetAnchors(coinPlate.rectTransform, new Vector2(0.04f, 0.91f), new Vector2(0.40f, 0.97f));
            coinText = CreateText(coinPlate.transform, "ЗОЛОТО  0", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Color(1f, 0.82f, 0.18f));

            runner.BeginRun();
        }

        private void ShowResult(bool won)
        {
            state = ScreenState.Result;
            if (gameplayHud != null) Destroy(gameplayHud);
            screenBackground.gameObject.SetActive(false);
            page = CreatePage(won ? "Victory" : "Defeat", new Color(0.025f, 0.04f, 0.08f, 0.92f));
            CreateText(page.transform, won ? "ДОСТАВКА\nВЫПОЛНЕНА!" : "ДОСТАВКА\nСОРВАНА", 65, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.78f), won ? new Color(0.5f, 1f, 0.72f) : new Color(1f, 0.47f, 0.38f));
            CreateText(page.transform, $"Собрано золота: {runner.Coins}", 32, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.49f), new Vector2(0.88f, 0.56f), Color.white);

            Button retry = CreateButton(page.transform, won ? "↻  ПРОЙТИ ЕЩЁ РАЗ" : "↻  ПОПРОБОВАТЬ ЕЩЁ", new Vector2(0.13f, 0.31f), new Vector2(0.87f, 0.40f), Ice, 30);
            retry.onClick.AddListener(ReloadIntoLevel);
            Button map = CreateButton(page.transform, "К КАРТЕ", new Vector2(0.13f, 0.20f), new Vector2(0.87f, 0.28f), new Color(0.18f, 0.22f, 0.34f), 30);
            map.onClick.AddListener(ShowMap);
        }

        private GameObject CreatePage(string name, Color color)
        {
            Image image = CreateImage(name, phone, color);
            Stretch(image.rectTransform);
            return image.gameObject;
        }

        private void ClearPage()
        {
            if (page != null) Destroy(page);
            if (gameplayHud != null) Destroy(gameplayHud);
            page = null;
            gameplayHud = null;
        }

        private Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max, Color color, int size = 38)
        {
            Image image = CreateImage(label + " Button", parent, color);
            SetAnchors(image.rectTransform, min, max);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            button.colors = colors;
            CreateText(image.transform, label, size, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), Color.white);
            return button;
        }

        private static void ApplyMenuButtonArt(Button button, string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return;

            Image image = button.GetComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                                         new Vector2(0.5f, 0.5f), 100f);
            image.type = Image.Type.Simple;
            image.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.9f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (button.GetComponent<MenuButtonMotion>() == null)
                button.gameObject.AddComponent<MenuButtonMotion>();
        }

        private Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Vector2 min, Vector2 max, Color color)
        {
            GameObject textObject = new(value + " Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetAnchors(rect, min, max);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
