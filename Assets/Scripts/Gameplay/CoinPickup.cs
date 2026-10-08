using UnityEngine;

namespace CourierRunner
{
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
}
