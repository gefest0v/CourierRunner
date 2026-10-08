using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Builds and owns everything in a run: the pooled model library, the moving world root with its
    /// segments, the courier and the camera. Also keeps coordinates small by shifting the world back
    /// toward the origin every few hundred meters (the courier never notices).
    /// </summary>
    public sealed class GameWorld : MonoBehaviour
    {
        public static GameWorld Current { get; private set; }

        /// <summary>Parent of all track content. Its local Z is the distance from the run start.</summary>
        public Transform Root { get; private set; }
        public ModelLibrary Library { get; private set; }
        public RunnerController Runner { get; private set; }
        public RunnerCamera RunnerCamera { get; private set; }
        public SegmentManager Segments { get; private set; }
        public DifficultyDirector Difficulty { get; private set; }
        public PatternGenerator Generator { get; private set; }
        public TreantEncounterManager Treants { get; private set; }
        public FlightDirector Flight { get; private set; }
        public Material GrassMaterial { get; private set; }
        public Material PathMaterial { get; private set; }
        /// <summary>True when the scene was reloaded to start a run right away (retry / level select).</summary>
        public bool AutoStart { get; private set; }

        /// <summary>The courier's distance from the run start in track coordinates.</summary>
        public float RunnerTrackZ => Root.InverseTransformPoint(Runner.transform.position).z;

        public event System.Action<float> OriginShifted;

        private GameConfig config;

        public static GameWorld Build()
        {
            var root = new GameObject("Courier Runner");
            GameWorld world = root.AddComponent<GameWorld>();
            world.BuildNow();
            return world;
        }

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void BuildNow()
        {
#if !UNITY_WEBGL
            // In the browser the frame loop follows requestAnimationFrame; capping it there causes stutter.
            Application.targetFrameRate = 60;
#endif
            Time.timeScale = 1f; // A run may have been left from the pause menu.
            config = GameConfig.Instance;
            AutoStart = GameSession.ConsumePendingRun();

            BuildEnvironment();
            Library = new ModelLibrary(transform);
            Root = new GameObject("World").transform;
            Root.SetParent(transform, false);
            CreateGroundMaterials();

            Runner = BuildRunner();
            RunnerCamera = BuildCamera(Runner.transform);
            new GameObject("Backdrop").AddComponent<Backdrop>().transform.SetParent(transform, false);

            Difficulty = new DifficultyDirector(config, GameSession.Mode);
            Generator = new PatternGenerator(config, Difficulty);
            Segments = gameObject.AddComponent<SegmentManager>();
            Segments.Initialize(this, Generator);
            int seed = GameSession.Mode == RunMode.Story ? config.storySeed : Random.Range(1, int.MaxValue);
            Treants = gameObject.AddComponent<TreantEncounterManager>();
            Treants.Initialize(this, seed);
            Flight = gameObject.AddComponent<FlightDirector>();
            Flight.Initialize(this, seed);
            Generator.SuppressObstacles = Flight.IsLandingZone;
            Segments.Begin(GameSession.Mode, seed);

            gameObject.AddComponent<GameFlowUI>().runner = Runner;
        }

        private static void BuildEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.6f);
            if (FindAnyObjectByType<Light>() == null)
            {
                Light sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }
            if (FindAnyObjectByType<CourierDistanceFog>() == null)
                new GameObject("Courier Distance Fog").AddComponent<CourierDistanceFog>();
        }

        private void CreateGroundMaterials()
        {
            Material grass = Resources.Load<Material>("Environment/Grass");
            GrassMaterial = grass != null ? grass : Library.ColorMaterial(new Color(0.16f, 0.27f, 0.095f));

            Material path = Resources.Load<Material>("Environment/Path");
            if (path == null)
            {
                PathMaterial = Library.ColorMaterial(new Color(0.16f, 0.19f, 0.25f));
                return;
            }
            // The path texture is mapped along each cube's length. Repeat it a whole number of
            // times per segment so the pattern lines up across segment joins.
            PathMaterial = new Material(path) { name = "Forest Path (segment)" };
            if (PathMaterial.HasProperty("_LengthRepeats"))
                PathMaterial.SetFloat("_LengthRepeats", Mathf.Max(1f, Mathf.Round(config.segmentLength / 20f)));
        }

        private RunnerController BuildRunner()
        {
            var player = new GameObject("Courier");
            player.transform.position = new Vector3(0f, 1.05f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.48f;
            controller.center = Vector3.zero;
            Library.CreateCourierVisual(player.transform);
            return player.AddComponent<RunnerController>();
        }

        private static RunnerCamera BuildCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            camera.fieldOfView = 62f;
            camera.farClipPlane = 400f;
            RunnerCamera follow = camera.GetComponent<RunnerCamera>();
            if (follow == null) follow = camera.gameObject.AddComponent<RunnerCamera>();
            follow.target = target;
            camera.transform.position = target.position + new Vector3(0f, 5f, -8f);
            return follow;
        }

        private void Update()
        {
            if (Runner.IsRunning) Runner.forwardSpeed = Difficulty.SpeedAt(RunnerTrackZ);
        }

        private void LateUpdate()
        {
            float runnerZ = Runner.transform.position.z;
            if (runnerZ < config.originShiftDistance) return;
            // Shift by whole segments: the grass and path textures are world-space and repeat
            // evenly within one segment, so the jump is invisible.
            float shift = Mathf.Floor(runnerZ / config.segmentLength) * config.segmentLength;
            ShiftOrigin(shift);
        }

        private void ShiftOrigin(float shift)
        {
            Vector3 offset = new(0f, 0f, -shift);
            Root.position += offset;

            CharacterController controller = Runner.GetComponent<CharacterController>();
            controller.enabled = false;
            Runner.transform.position += offset;
            controller.enabled = true;

            RunnerCamera.transform.position += offset;
            Physics.SyncTransforms();
            foreach (TowerProjectile projectile in Root.GetComponentsInChildren<TowerProjectile>())
                projectile.ClearTrail();
            OriginShifted?.Invoke(shift);
        }
    }
}
