using UnityEngine;

namespace CourierRunner
{
    /// <summary>Fires a fireball down its lane while the courier is in the same lane and within range.</summary>
    public sealed class TowerShooter : MonoBehaviour, IPoolable
    {
        public float detectionDistance = 20f;
        public float fireInterval = 1.25f;
        private float cooldown = 0.5f;

        public void OnTakenFromPool()
        {
            cooldown = 0.5f;
            GameWorld world = GameWorld.Current;
            if (world != null && world.Difficulty != null)
                fireInterval = world.Difficulty.TowerFireIntervalAt(world.Root.InverseTransformPoint(transform.position).z);
        }

        public void OnReturnedToPool() { }

        private void Update()
        {
            GameWorld world = GameWorld.Current;
            if (world == null || world.Runner == null || !world.Runner.IsRunning) return;
            cooldown -= Time.deltaTime;
            Vector3 runnerPosition = world.Runner.transform.position;
            Vector3 position = transform.position;
            float forwardDistance = position.z - runnerPosition.z;
            bool sameLane = Mathf.Abs(position.x - runnerPosition.x) < 1.15f;
            if (!sameLane || forwardDistance <= 1.5f || forwardDistance > detectionDistance || cooldown > 0f)
                return;

            cooldown = fireInterval;
            Vector3 spawn = new Vector3(position.x, 1.25f, position.z - 1.3f);
            world.Library.Get(PoolKeys.Fireball, world.Root, world.Root.InverseTransformPoint(spawn));
        }
    }
}
