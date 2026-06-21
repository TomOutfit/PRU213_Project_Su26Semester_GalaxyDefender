using UnityEngine;

/// <summary>
/// Player projectile. Travels straight up at <see cref="speed"/>; on hitting an Enemy or Boss
/// it deals <see cref="damage"/>, spawns a hit effect, and returns itself to the pool. Also
/// returns to the pool when it leaves the screen.
/// </summary>
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
        RuntimeSpriteFixer.EnsureSprite(GetComponent<SpriteRenderer>(), "Assets/Sprites/Bullets/bullet_player.png");
        pool = GetComponentInParent<ObjectPool>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = Vector2.up * speed;
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
            GameObject poolObj = GameObject.Find("HitEffectPool");
            if (poolObj != null)
            {
                ObjectPool hitPool = poolObj.GetComponent<ObjectPool>();
                if (hitPool != null)
                {
                    hitPool.Get(transform.position, Quaternion.identity);
                }
            }
            ReturnToPool();
        }
    }

    private void OnBecameInvisible()
    {
        ReturnToPool();
    }

    /// <summary>Releases this bullet back to its pool, or deactivates it if unpooled.</summary>
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
