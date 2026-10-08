using UnityEngine;

namespace CourierRunner
{
    /// <summary>Floating wings power-up. Touching it starts (or extends) a flight.</summary>
    public sealed class WingsPickup : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner == null || !runner.IsRunning) return;
            runner.StartFlight(FlightDuration);
            PooledObject.ReleaseOrDestroy(gameObject);
        }

        /// <summary>Base duration plus the purchased duration upgrades.</summary>
        public static float FlightDuration
        {
            get
            {
                GameConfig config = GameConfig.Instance;
                return config.flightDuration + SaveSystem.Data.flightLevel * config.flightUpgradeSeconds;
            }
        }
    }
}
