using UnityEngine;

/// <summary>
/// Enemy projectile. Travels straight down at <see cref="speed"/>; on hitting the Player it
/// deals <see cref="damage"/> (with knockback via the hit-source overload) and returns to the
/// pool. Also returns to the pool when it leaves the screen.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BulletEnemy : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 50;
    
    private Rigidbody2D rb;
    private ObjectPool pool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    [HideInInspector]
    public string customBulletSpritePath;

    public void SetSpritePath(string path)
    {
        customBulletSpritePath = path;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && !string.IsNullOrEmpty(path))
        {
            RuntimeSpriteFixer.EnsureSprite(sr, path, true);
        }
    }

    private void Start()
    {
        pool = GetComponentInParent<ObjectPool>();
    }

    private void OnEnable()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            string path = string.IsNullOrEmpty(customBulletSpritePath) ? "Assets/Sprites/Bullets/bullet_enemy.png" : customBulletSpritePath;
            RuntimeSpriteFixer.EnsureSprite(sr, path, true);
        }
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
