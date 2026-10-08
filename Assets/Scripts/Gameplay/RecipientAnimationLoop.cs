using UnityEngine;

namespace CourierRunner
{
    /// <summary>Restarts the recipient's idle clip, which is imported without looping.</summary>
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
}
