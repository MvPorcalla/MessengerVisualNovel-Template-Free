// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/Common/Pooling/PoolingManager.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using ChatSim.Core;

namespace ChatSim.UI.Common.Pooling
{
    /// <summary>
    /// Generic object pooling manager for efficient reuse of UI elements and game objects.
    /// Attach to: ChatAppController GameObject
    /// Each spawner (ChatMessageSpawner, ChatChoiceSpawner) tracks its own active objects.
    /// PoolingManager only manages the inactive pool — it does not track what is currently in use.
    /// </summary>
    public class PoolingManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ POOL STORAGE
        // ═══════════════════════════════════════════════════════════

        private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        private Transform poolRoot;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "PoolingManager",
            () => GameBootstrap.Config?.poolingManagerDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            poolRoot = new GameObject("_PooledObjects").transform;
            poolRoot.SetParent(transform);
            poolRoot.gameObject.SetActive(false);

            _log.Info("Initialized");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Get an object from pool or create a new one.
        /// Caller is responsible for tracking active objects.
        /// </summary>
        public GameObject Get(GameObject prefab, Transform parent = null, bool activateOnGet = false)
        {
            if (prefab == null)
            {
                _log.Error("Cannot get object from null prefab");
                return null;
            }

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            GameObject obj;

            if (pools[prefab].Count > 0)
            {
                obj = pools[prefab].Dequeue();
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = Instantiate(prefab, parent);

                var pooledObject = obj.GetComponent<PooledObject>();
                if (pooledObject == null)
                    pooledObject = obj.AddComponent<PooledObject>();

                pooledObject.SetPrefab(prefab);
                pooledObject.PreserveContent = true;
            }

            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            if (activateOnGet)
                obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// Return object to pool.
        /// Caller should call ResetForPool() on the component before recycling.
        /// </summary>
        public void Recycle(GameObject obj)
        {
            if (obj == null) return;

            if (poolRoot == null)
            {
                _log.Warn("Recycle called before Awake — destroying object instead");
                Destroy(obj);
                return;
            }

            var pooledObject = obj.GetComponent<PooledObject>();
            if (pooledObject == null || pooledObject.Prefab == null)
            {
                _log.Warn($"Cannot recycle {obj.name} - no PooledObject component");
                Destroy(obj);
                return;
            }

            GameObject prefab = pooledObject.Prefab;

            if (!pooledObject.PreserveContent)
                ClearDynamicContent(obj);

            obj.SetActive(false);
            obj.transform.SetParent(poolRoot, false);

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            pools[prefab].Enqueue(obj);
        }

        /// <summary>
        /// Pre-warm pool with inactive instances to avoid instantiation spikes at runtime.
        /// </summary>
        public void PreWarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, poolRoot);
                obj.SetActive(false);

                var pooledObject = obj.GetComponent<PooledObject>();
                if (pooledObject == null)
                    pooledObject = obj.AddComponent<PooledObject>();

                pooledObject.SetPrefab(prefab);
                pooledObject.PreserveContent = true;
                pools[prefab].Enqueue(obj);
            }

            _log.Info($"Pre-warmed {count} instances of {prefab.name}");
        }

        /// <summary>
        /// Destroy all pooled objects and clear all pools.
        /// Does not affect active objects — callers must recycle or destroy those themselves.
        /// </summary>
        public void ClearAllPools()
        {
            int totalDestroyed = 0;

            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                        totalDestroyed++;
                    }
                }
            }

            pools.Clear();
            _log.Info($"Cleared all pools - destroyed {totalDestroyed} objects");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Fallback content clear for objects that don't implement ResetForPool().
        /// Clears all TMP text and root button listeners.
        /// </summary>
        private void ClearDynamicContent(GameObject obj)
        {
            // Always fires — indicates a real programming mistake
            _log.Error($"{obj.name} recycled without ResetForPool() implementation. " +
                    $"Add ResetForPool() to the component or set PreserveContent = true on its PooledObject.");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
}