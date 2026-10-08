using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// A Nature's Prophet treant charging down one lane toward the courier, like the oncoming train in
    /// Subway Surfers. Lives in the pool until its encounter starts, then walks until it is behind the camera.
    /// </summary>
    public sealed class Treant : MonoBehaviour, IPoolable
    {
        public const float Height = 3.6f;
        private const float HalfWidth = 1.15f;
        private const float HalfDepth = 0.7f;

        private float speed;
        private float phase;
        private Transform body;
        private ParticleSystem dust;

        private void Awake()
        {
            body = transform.Find("Body");
            dust = GetComponentInChildren<ParticleSystem>();
        }

        public void Launch(float walkSpeed)
        {
            speed = walkSpeed;
            phase = Random.Range(0f, Mathf.PI * 2f);
            if (dust != null) dust.Play();
        }

        public void OnTakenFromPool() => speed = 0f;

        public void OnReturnedToPool()
        {
            if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            transform.position += Vector3.back * (speed * Time.deltaTime);
            AnimateStride();

            GameWorld world = GameWorld.Current;
            if (world == null || world.Runner == null) return;
            RunnerController runner = world.Runner;
            Vector3 offset = runner.transform.position - transform.position;
            // offset = courier - treant, so a large positive Z means the treant is well behind the courier.
            if (offset.z > 25f)
            {
                PooledObject.ReleaseOrDestroy(gameObject);
                return;
            }
            // The courier capsule bottom is about one meter below its center. A trigger alone can
            // miss a fast head-on approach, so overlap is also checked directly.
            float runnerBottom = runner.transform.position.y - 1f;
            if (runner.IsRunning && Mathf.Abs(offset.x) < HalfWidth + 0.4f && Mathf.Abs(offset.z) < HalfDepth + 0.5f &&
                runnerBottom < Height)
                runner.Crash();
        }

        /// <summary>Heavy, stomping walk: the body bobs on each step and sways side to side.</summary>
        private void AnimateStride()
        {
            if (body == null) return;
            float stepRate = 1.2f + speed * 0.18f;
            float t = Time.time * stepRate * Mathf.PI + phase;
            body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t)) * 0.28f, 0f);
            body.localRotation = Quaternion.Euler(Mathf.Sin(t * 2f) * 3f, 0f, Mathf.Sin(t) * 6f);
        }

        private void OnTriggerEnter(Collider other)
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner != null && runner.transform.position.y - 1f < Height) runner.Crash();
        }
    }
}
