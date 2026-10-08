using UnityEngine;

namespace CourierRunner
{
    /// <summary>Ends the run when the courier touches this trigger volume.</summary>
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
}
