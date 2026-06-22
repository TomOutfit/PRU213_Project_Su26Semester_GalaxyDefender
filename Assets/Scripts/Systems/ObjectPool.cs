using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic GameObject pool that reuses instances instead of Instantiate/Destroy to avoid GC
/// allocation during sustained spawning (bullets, explosions). Hard-capped at
/// <see cref="maxCapacity"/>; once full, the oldest active object is recycled.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [Tooltip("The prefab this pool manages")]
    public GameObject prefab;
    
    [Tooltip("Maximum active objects before recycling the oldest")]
    public int maxCapacity = 20;

    private Queue<GameObject> availableObjects = new Queue<GameObject>();
    private LinkedList<GameObject> activeObjects = new LinkedList<GameObject>();

    /// <summary>
    /// Activates a pooled instance at the given pose. Reuses a free object if available,
    /// otherwise instantiates one until <see cref="maxCapacity"/> is reached, then recycles
    /// the oldest active object.
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = null;

        while (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
            if (obj != null) break;
        }

        if (obj == null)
        {
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
                // Auto-return oldest
                while (activeObjects.Count > 0)
                {
                    obj = activeObjects.First.Value;
                    activeObjects.RemoveFirst();
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        break;
                    }
                }
            }
        }

        if (obj == null)
        {
            // If we still don't have an object (e.g., all active objects were destroyed or prefab is null)
            // try to instantiate a new one regardless of maxCapacity if needed, or return null.
            if (prefab != null)
            {
                obj = Instantiate(prefab, transform);
            }
            else
            {
                return null;
            }
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        activeObjects.AddLast(obj);
        
        return obj;
    }

    /// <summary>Deactivates an object and returns it to the free queue for reuse.</summary>
    public void Release(GameObject obj)
    {
        if (obj.activeSelf)
        {
            obj.SetActive(false);
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }
    }
}
