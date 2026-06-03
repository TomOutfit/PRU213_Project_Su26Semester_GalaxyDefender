using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletPlayer : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 10;
    
    private Rigidbody2D rb;
    private ObjectPool pool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        pool = GetComponentInParent<ObjectPool>();
    }

    private void FixedUpdate()
    {
        rb.velocity = Vector2.up * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") || collision.CompareTag("Boss"))
        {
            EnemyHealth health = collision.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            ReturnToPool();
        }
    }

    private void OnBecameInvisible()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
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
