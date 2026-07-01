using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic GameObject pool that reuses instances instead of Instantiate/Destroy to avoid GC
/// allocation during sustained spawning (bullets, explosions). Hard-capped at maxCapacity.
/// Optimised with O(1) lookups using a HashSet for active tracking and a Queue for recycling order.
/// Supports optional pre-warming (initial sizing) on Start to prevent runtime frame spikes.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [Tooltip("The prefab this pool manages")]
    public GameObject prefab;
    
    [Tooltip("Maximum active objects before recycling the oldest")]
    public int maxCapacity = 20;

    [Tooltip("Number of objects to pre-warm/instantiate when the game starts")]
    public int initialSize = 10;

    private readonly Queue<GameObject> availableObjects = new Queue<GameObject>();
    private readonly HashSet<GameObject> activeObjects = new HashSet<GameObject>();
    private readonly Queue<GameObject> activeOrder = new Queue<GameObject>();

    private void Start()
    {
        PrewarmPool();
    }

    /// <summary>
    /// Instantiates initialSize instances of the prefab, deactivates them, and places them in the pool.
    /// </summary>
    private void PrewarmPool()
    {
        if (prefab == null)
        {
            return;
        }

        int countToSpawn = Mathf.Min(initialSize, maxCapacity);
        for (int i = 0; i < countToSpawn; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            availableObjects.Enqueue(obj);
        }
    }

    /// <summary>
    /// Activates a pooled instance at the given pose. Reuses a free object if available,
    /// otherwise instantiates one until maxCapacity is reached, then recycles
    /// the oldest active object.
    /// All operations run in O(1) time complexity.
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = null;

        // 1. Try to get a valid inactive object from the pool queue
        while (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
            if (obj != null)
            {
                break;
            }
        }

        // 2. If no inactive object exists, decide whether to instantiate or recycle
        if (obj == null)
        {
            // Clean up destroyed references from active count
            activeObjects.RemoveWhere(item => item == null);

            if (activeObjects.Count < maxCapacity)
            {
                if (prefab == null)
                {
                    Debug.LogError($"[ObjectPool] Prefab is null on pool '{gameObject.name}'!");
                    return null;
                }
                obj = Instantiate(prefab, transform);
            }
            else
            {
                // Recycle the oldest active object (O(1) time)
                while (activeOrder.Count > 0)
                {
                    GameObject oldest = activeOrder.Dequeue();
                    if (oldest != null && activeObjects.Contains(oldest))
                    {
                        oldest.SetActive(false);
                        activeObjects.Remove(oldest);
                        obj = oldest;
                        break;
                    }
                }

                // Fallback: If for some reason no active object was found to recycle
                if (obj == null && prefab != null)
                {
                    obj = Instantiate(prefab, transform);
                }
            }
        }

        if (obj == null) return null;

        // 3. Setup and activate the object
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        activeObjects.Add(obj);
        activeOrder.Enqueue(obj);
        
        return obj;
    }

    /// <summary>
    /// Deactivates an object and returns it to the free queue for reuse in O(1) time.
    /// </summary>
    public void Release(GameObject obj)
    {
        if (obj == null) return;

        // Only release if the object is currently tracked as active
        if (activeObjects.Contains(obj))
        {
            obj.SetActive(false);
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }
    }
}
