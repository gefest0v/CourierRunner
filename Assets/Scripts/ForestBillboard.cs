using UnityEngine;

namespace CourierRunner
{
    // Rotates only the section root. Imported model correction remains on the child tree.
    public sealed class ForestBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            Transform target = null;
            GameWorld world = GameWorld.Current;
            if (world != null && world.Runner != null) target = world.Runner.transform;
            else if (Camera.main != null) target = Camera.main.transform;
            if (target == null) return;
            Vector3 direction = transform.position - target.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
