using UnityEngine;

public class BulletEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 10f;
    public int damage = 10;

    [HideInInspector]
    public ObjectPool pool;

    private void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Release();
        }
    }

    private void Release()
    {
        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        Release();
    }
}
