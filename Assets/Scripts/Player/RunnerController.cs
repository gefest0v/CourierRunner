using UnityEngine;

namespace CourierRunner
{
    /// <summary>Moves the courier forward along three lanes and handles jumping, crashing and delivery.</summary>
    [RequireComponent(typeof(CharacterController))]
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
        public int Lane => lane;

        private CharacterController controller;
        private int lane;
        private float verticalSpeed;
        private Animator animator;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            GameConfig config = GameConfig.Instance;
            forwardSpeed = config.startSpeed;
            laneWidth = config.laneWidth;
            laneChangeSpeed = config.laneChangeSpeed;
            jumpHeight = config.jumpHeight;
            if (GetComponent<RunnerInput>() == null) gameObject.AddComponent<RunnerInput>();
        }

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
                animator.applyRootMotion = false;
        }

        private void Update()
        {
            KeepLoopingAnimation();
            if (!IsRunning) return;

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

        public void BeginRun()
        {
            HasStarted = true;
            IsRunning = true;
        }

        public void MoveLeft() { if (IsRunning) lane = Mathf.Clamp(lane - 1, -1, 1); }
        public void MoveRight() { if (IsRunning) lane = Mathf.Clamp(lane + 1, -1, 1); }

        public void Jump()
        {
            if (IsRunning && controller.isGrounded)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        }

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
}
