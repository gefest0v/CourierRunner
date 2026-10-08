using UnityEngine;

namespace CourierRunner
{
    /// <summary>Flaps a pair of wing pivots. Faster flaps while climbing, slow glides when cruising.</summary>
    public sealed class WingFlap : MonoBehaviour
    {
        public Transform left;
        public Transform right;
        public float flapsPerSecond = 2.2f;
        public float amplitude = 32f;

        private void Update()
        {
            float angle = Mathf.Sin(Time.time * flapsPerSecond * Mathf.PI * 2f) * amplitude;
            if (left != null) left.localRotation = Quaternion.Euler(0f, -18f, -angle);
            if (right != null) right.localRotation = Quaternion.Euler(0f, 18f, angle);
        }
    }
}
