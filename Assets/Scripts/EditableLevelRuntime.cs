using UnityEngine;

namespace CourierRunner
{
    // Stable scene component. Gameplay behaviours are attached at runtime because the prototype
    // keeps several MonoBehaviour classes in one source file, which Unity cannot persist reliably.
    [DefaultExecutionOrder(-10000)]
    public sealed class EditableLevelRuntime : MonoBehaviour
    {
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ForceInitializeEditableScene()
        {
            EditableLevelRuntime runtime = FindFirstObjectByType<EditableLevelRuntime>(FindObjectsInactive.Include);
            if (runtime == null)
            {
                foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (root.name != "Courier Runner Level") continue;
                    runtime = root.AddComponent<EditableLevelRuntime>();
                    break;
                }
            }
            if (runtime != null) runtime.InitializeNow();
        }

        private void Awake() => InitializeNow();

        public void InitializeNow()
        {
            if (initialized) return;
            RunnerController runner = FindFirstObjectByType<RunnerController>();
            if (runner == null)
            {
                GameObject courier = FindNamed("Courier");
                if (courier == null) return;
                RefreshCourierVisual(courier);
                runner = courier.GetComponent<RunnerController>() ?? courier.AddComponent<RunnerController>();
            }
            // Keep gameplay tuning stable even when an older scene/template serialized
            // a previous public-field value on RunnerController.
            runner.jumpHeight = 2.15f;

            Camera camera = Camera.main;
            if (camera != null)
            {
                RunnerCamera follow = camera.GetComponent<RunnerCamera>() ?? camera.gameObject.AddComponent<RunnerCamera>();
                follow.target = runner.transform;
            }

            GameFlowUI flow = GetComponent<GameFlowUI>() ?? gameObject.AddComponent<GameFlowUI>();
            flow.runner = runner;

            foreach (Transform item in GetComponentsInChildren<Transform>(true))
            {
                string objectName = item.name;
                if (objectName.EndsWith(" Hitbox") && item.GetComponent<ObstacleTrigger>() == null)
                    item.gameObject.AddComponent<ObstacleTrigger>();
                else if (objectName == "Gold")
                {
                    if (item.GetComponent<CoinPickup>() == null)
                        item.gameObject.AddComponent<CoinPickup>();
                    float z = item.position.z;
                    if (Mathf.Abs(z - 72f) < 0.1f || Mathf.Abs(z - 119f) < 0.1f)
                    {
                        Vector3 position = item.position;
                        position.y = 2.65f;
                        item.position = position;
                    }
                }
                else if (objectName == "Finish Trigger" || objectName == "Delivery Finish")
                {
                    ConfigureFinish(item.gameObject, runner);
                }
                else if (objectName == "Lane Tower")
                {
                    TowerShooter tower = item.GetComponent<TowerShooter>() ?? item.gameObject.AddComponent<TowerShooter>();
                    tower.runner = runner;
                    tower.laneX = item.position.x;
                    tower.towerZ = item.position.z;
                }
            }

            // The editable scene keeps the finish as a root object, outside this component's
            // hierarchy, so it must also be located globally.
            GameObject finishObject = GameObject.Find("Delivery Finish") ?? GameObject.Find("Finish Trigger");
            if (finishObject != null)
                ConfigureFinish(finishObject, runner);

            GameObject recipient = FindNamed("Crystal Maiden Recipient");
            if (recipient != null)
            {
                RecipientAnimationLoop loop = recipient.GetComponent<RecipientAnimationLoop>() ??
                                              recipient.AddComponent<RecipientAnimationLoop>();
                loop.animator = recipient.GetComponentInChildren<Animator>();
            }
            initialized = true;
            Debug.Log("EditableLevelRuntime initialized: menu, camera and gameplay handlers restored.");
        }

        private GameObject FindNamed(string objectName)
        {
            GameObject sceneObject = GameObject.Find(objectName);
            if (sceneObject != null) return sceneObject;
            foreach (Transform item in GetComponentsInChildren<Transform>(true))
                if (item.name == objectName) return item.gameObject;
            return null;
        }

        private static void ConfigureFinish(GameObject finishObject, RunnerController runner)
        {
            BoxCollider trigger = finishObject.GetComponent<BoxCollider>() ?? finishObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            // Make the finish deep enough that a fast runner cannot skip it between physics ticks.
            trigger.size = new Vector3(Mathf.Max(11f, trigger.size.x), Mathf.Max(4f, trigger.size.y),
                                       Mathf.Max(4f, trigger.size.z));
            FinishTrigger finish = finishObject.GetComponent<FinishTrigger>() ??
                                   finishObject.AddComponent<FinishTrigger>();
            finish.runner = runner;
        }

        private static void RefreshCourierVisual(GameObject courier)
        {
            GameObject visualPrefab = Resources.Load<GameObject>("CourierVisual");
            if (visualPrefab == null) return;
            for (int i = courier.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = courier.transform.GetChild(i);
                // Older scene versions stored the same visual under "Courier Model" and
                // "Greevil Model". Remove every rendered/model child before adding one clean
                // prefab, otherwise the static pink copy remains under the animated one.
                if (child.name.Contains("CourierVisual") || child.name.Contains("Courier Model") ||
                    child.name.Contains("Greevil") || child.GetComponentInChildren<Renderer>(true) != null ||
                    child.GetComponentInChildren<Animator>(true) != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
            GameObject visual = Instantiate(visualPrefab, courier.transform);
            visual.name = "CourierVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
        }
    }
}
