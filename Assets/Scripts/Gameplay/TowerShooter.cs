using UnityEngine;

namespace CourierRunner
{
    /// <summary>Fires a fireball down its lane while the courier is in the same lane and within range.</summary>
    public sealed class TowerShooter : MonoBehaviour
    {
        public RunnerController runner;
        public float laneX;
        public float towerZ;
        public float detectionDistance = 20f;
        public float fireInterval = 1.25f;
        private float cooldown = 0.5f;

        private void Update()
        {
            if (runner == null || !runner.IsRunning) return;
            cooldown -= Time.deltaTime;
            float forwardDistance = towerZ - runner.transform.position.z;
            bool sameLane = Mathf.Abs(laneX - runner.transform.position.x) < 1.15f;
            if (!sameLane || forwardDistance <= 1.5f || forwardDistance > detectionDistance || cooldown > 0f)
                return;

            cooldown = fireInterval;
            TowerProjectile.Spawn(new Vector3(laneX, 1.25f, towerZ - 1.3f));
        }
    }
}
