using UnityEngine;

public class ReturnToPoolHelper : MonoBehaviour
{
    private ObjectPool pool;

    private void Start()
    {
        pool = GetComponentInParent<ObjectPool>();
    }

    public void ReturnToPool()
    {
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
