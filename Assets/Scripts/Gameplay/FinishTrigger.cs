using UnityEngine;

namespace CourierRunner
{
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
}
