using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletBoss : MonoBehaviour
{
    public float speed = 8f;
    public int damage = 10;
    public string spritePath = "Assets/Sprites/Bullets/bullet_boss.png";

    private Rigidbody2D rb;
    private ObjectPool pool;
    private Vector2 direction = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        RuntimeSpriteFixer.EnsureSprite(GetComponent<SpriteRenderer>(), spritePath);
        pool = GetComponentInParent<ObjectPool>();
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        transform.up = direction;
    }

    private void FixedUpdate()
    {
        // Unity 2023+ uses linearVelocity instead of velocity. Supporting both fallback options:
        #if UNITY_2023_1_OR_NEWER
        rb.linearVelocity = direction.normalized * speed;
        #else
        rb.velocity = direction.normalized * speed;
        #endif
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
