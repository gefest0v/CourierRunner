using UnityEngine;
using UnityEngine.InputSystem;

namespace CourierRunner
{
    /// <summary>Keyboard (A/D, arrows, W/Space) and swipe controls for <see cref="RunnerController"/>.</summary>
    [RequireComponent(typeof(RunnerController))]
    public sealed class RunnerInput : MonoBehaviour
    {
        private RunnerController runner;
        private Vector2 touchStart;
        private bool trackingTouch;

        private void Awake() => runner = GetComponent<RunnerController>();

        private void Update()
        {
            if (!runner.IsRunning)
            {
                trackingTouch = false;
                return;
            }
            ReadKeyboard();
            ReadTouch();
        }

        private void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) runner.MoveLeft();
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) runner.MoveRight();
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame)
                runner.Jump();
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
            if (swipe.magnitude < GameConfig.Instance.swipeThreshold) return;
            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            {
                if (swipe.x > 0f) runner.MoveRight();
                else runner.MoveLeft();
            }
            else if (swipe.y > 0f)
            {
                runner.Jump();
            }
            // A downward swipe is intentionally unused: the game has no slide action yet.
        }
    }
}
