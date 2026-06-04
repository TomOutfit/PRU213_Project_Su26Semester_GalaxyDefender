using UnityEngine;

public class ReturnToPoolOnDisable : MonoBehaviour
{
    [Tooltip("Time before returning to pool")]
    public float delay = 0.5f;
    
    private ObjectPool pool;
    private string poolName;

    private void Awake()
    {
        poolName = gameObject.name.Replace("(Clone)", "") + "Pool";
    }

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(ReturnToPool), delay);
    }

    private void ReturnToPool()
    {
        if (pool == null)
        {
            pool = GameObject.Find(poolName)?.GetComponent<ObjectPool>();
        }

        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
