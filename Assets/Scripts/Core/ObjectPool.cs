using System;
using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>Optional callbacks for pooled objects that must reset their state on reuse.</summary>
    public interface IPoolable
    {
        void OnTakenFromPool();
        void OnReturnedToPool();
    }

    /// <summary>Marks an instance as owned by a pool so any script can hand it back with <see cref="Release"/>.</summary>
    public sealed class PooledObject : MonoBehaviour
    {
        public ObjectPool Pool { get; internal set; }
        public bool InPool { get; internal set; }

        public void Release()
        {
            if (Pool != null) Pool.Release(this);
            else Destroy(gameObject);
        }

        /// <summary>Returns a pooled object to its pool, or destroys an object that was never pooled.</summary>
        public static void ReleaseOrDestroy(GameObject target)
        {
            if (target == null) return;
            PooledObject pooled = target.GetComponent<PooledObject>();
            if (pooled != null) pooled.Release();
            else Destroy(target);
        }
    }

    /// <summary>
    /// Reuses inactive copies instead of instantiating and destroying objects during a run.
    /// Inactive instances are parked under <c>storage</c>.
    /// </summary>
    public sealed class ObjectPool
    {
        private readonly Func<GameObject> factory;
        private readonly Transform storage;
        private readonly Stack<PooledObject> free = new();
        private readonly List<IPoolable> callbacks = new();
        private Vector3 templateScale = Vector3.one;

        public string Name { get; }
        public int CreatedCount { get; private set; }
        public int ActiveCount => CreatedCount - free.Count;

        public ObjectPool(string name, Func<GameObject> factory, Transform storage)
        {
            Name = name;
            this.factory = factory;
            this.storage = storage;
        }

        public void Prewarm(int count)
        {
            for (int i = CreatedCount; i < count; i++)
                Release(Create());
        }

        public GameObject Get(Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            PooledObject item = free.Count > 0 ? free.Pop() : Create();
            item.InPool = false;
            Transform itemTransform = item.transform;
            itemTransform.SetParent(parent, false);
            itemTransform.localPosition = localPosition;
            itemTransform.localRotation = localRotation;
            itemTransform.localScale = templateScale;
            item.gameObject.SetActive(true);
            item.GetComponents(callbacks);
            foreach (IPoolable poolable in callbacks) poolable.OnTakenFromPool();
            return item.gameObject;
        }

        public void Release(PooledObject item)
        {
            if (item == null || item.InPool) return;
            item.GetComponents(callbacks);
            foreach (IPoolable poolable in callbacks) poolable.OnReturnedToPool();
            item.InPool = true;
            item.gameObject.SetActive(false);
            item.transform.SetParent(storage, false);
            free.Push(item);
        }

        private PooledObject Create()
        {
            GameObject instance = factory();
            instance.name = Name;
            if (CreatedCount == 0) templateScale = instance.transform.localScale;
            PooledObject item = instance.GetComponent<PooledObject>();
            if (item == null) item = instance.AddComponent<PooledObject>();
            item.Pool = this;
            CreatedCount++;
            return item;
        }
    }
}
