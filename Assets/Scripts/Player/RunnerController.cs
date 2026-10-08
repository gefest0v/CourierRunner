using System;
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
        [Tooltip("Ground distance covered by a jump. Constant at every speed, so faster runs mean shorter, snappier jumps.")]
        public float jumpLength = 11f;

        public int Coins { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasStarted { get; private set; }
        public bool HasDelivered { get; private set; }
        public int Lane => lane;

        /// <summary>Raised with the flight duration whenever wings are picked up.</summary>
        public event Action<float> FlightStarted;
        public event Action FlightEnded;
        public bool IsFlying { get; private set; }
        public float FlightRemaining { get; private set; }
        public float FlightDuration { get; private set; }
        /// <summary>0 on the ground, 1 at full flight altitude. Drives the camera.</summary>
        public float FlightBlend => Mathf.Clamp01((transform.position.y - GroundCenterY) / Mathf.Max(0.1f, flightHeight));

        /// <summary>Capsule center height when standing on the road.</summary>
        private const float GroundCenterY = 1.05f;
        private float flightHeight = 6f;
        private float altitudeVelocity;
        private float invulnerableUntil;
        private GameObject wingsVisual;

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
            jumpLength = config.jumpLength;
            flightHeight = config.flightHeight;
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

            float vertical;
            if (IsFlying)
            {
                vertical = UpdateFlight();
            }
            else
            {
                if (controller.isGrounded && verticalSpeed < 0f)
                    verticalSpeed = -1f;
                verticalSpeed -= Gravity * Time.deltaTime;
                vertical = verticalSpeed * Time.deltaTime;
            }

            float targetX = lane * laneWidth;
            float horizontal = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime)
                               - transform.position.x;
            controller.Move(new Vector3(horizontal, vertical, forwardSpeed * Time.deltaTime));
        }

        /// <summary>Climbs to flight altitude, cruises, then glides back down when time runs out.</summary>
        private float UpdateFlight()
        {
            FlightRemaining = Mathf.Max(0f, FlightRemaining - Time.deltaTime);
            bool climbing = FlightRemaining > 0f;
            float targetY = climbing ? GroundCenterY + flightHeight : GroundCenterY - 0.2f;
            float y = transform.position.y;
            float newY = Mathf.SmoothDamp(y, targetY, ref altitudeVelocity, climbing ? 0.45f : 0.55f);
            if (!climbing && (controller.isGrounded || newY <= GroundCenterY + 0.05f))
            {
                EndFlight();
                return -1f * Time.deltaTime;
            }
            return newY - y;
        }

        public void StartFlight(float duration)
        {
            if (!IsRunning) return;
            IsFlying = true;
            FlightDuration = duration;
            FlightRemaining = Mathf.Max(FlightRemaining, duration);
            verticalSpeed = 0f;
            if (wingsVisual != null) wingsVisual.SetActive(true);
            FlightStarted?.Invoke(FlightRemaining);
        }

        /// <summary>
        /// Gives the courier its (hidden) wings. Built up front: creating objects inside the pickup's
        /// trigger callback is not allowed to destroy helper colliders immediately.
        /// </summary>
        public void AttachWings(GameObject visual)
        {
            wingsVisual = visual;
            wingsVisual.transform.SetParent(transform, false);
            wingsVisual.transform.localPosition = new Vector3(0f, 0.35f, -0.3f);
            wingsVisual.transform.localScale = Vector3.one * 0.9f;
            wingsVisual.SetActive(false);
        }

        private void EndFlight()
        {
            IsFlying = false;
            FlightRemaining = 0f;
            altitudeVelocity = 0f;
            verticalSpeed = -1f;
            // A short grace period so a landing next to an obstacle is never an instant loss.
            invulnerableUntil = Time.time + 0.6f;
            if (wingsVisual != null) wingsVisual.SetActive(false);
            FlightEnded?.Invoke();
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
            // Apex height h reached after half the air time T: v0 = 4h / T.
            if (IsRunning && !IsFlying && controller.isGrounded)
                verticalSpeed = 4f * jumpHeight / AirTime;
        }

        private float AirTime => jumpLength / Mathf.Max(1f, forwardSpeed);

        /// <summary>Gravity that lands a jump exactly <see cref="jumpLength"/> meters later: g = 8h / T².</summary>
        private float Gravity => 8f * jumpHeight / (AirTime * AirTime);

        public void AddCoin() => Coins++;

        public void Crash()
        {
            if (!IsRunning || Time.time < invulnerableUntil) return;
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
