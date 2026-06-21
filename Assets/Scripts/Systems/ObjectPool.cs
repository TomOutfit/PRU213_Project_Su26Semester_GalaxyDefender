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
        GameObject obj;

        if (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
        }
        else if (activeObjects.Count < maxCapacity)
        {
            obj = Instantiate(prefab, transform);
        }
        else
        {
            // Auto-return oldest
            obj = activeObjects.First.Value;
            activeObjects.RemoveFirst();
            obj.SetActive(false);
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
