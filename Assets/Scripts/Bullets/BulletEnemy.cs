using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletEnemy : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    
    private Rigidbody2D rb;
    private ObjectPool pool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        RuntimeSpriteFixer.EnsureSprite(GetComponent<SpriteRenderer>(), "Assets/Sprites/Bullets/bullet_enemy.png");
        pool = GetComponentInParent<ObjectPool>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = Vector2.down * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
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
