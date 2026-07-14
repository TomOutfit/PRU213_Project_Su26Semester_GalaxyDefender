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
    public int damage = 100000;
    
    private Rigidbody2D rb;
    private ObjectPool pool;
    private TrailRenderer trail;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Add a neon-trail for artistic player bullets
        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.12f;
            trail.endWidth = 0.0f;
            trail.sortingOrder = 4;
            
            // Bright neon yellow/orange trail gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.2f, 1f), 0f), 
                    new GradientColorKey(new Color(1f, 0.5f, 0f, 0.3f), 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.8f, 0f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            trail.colorGradient = gradient;
            
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                trail.material = new Material(spriteShader);
            }
        }
    }

    private void OnEnable()
    {
        if (trail != null)
        {
            trail.Clear();
        }
    }

    private void Start()
    {
        damage = 100000;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            RuntimeSpriteFixer.EnsureSprite(sr, "Assets/Sprites/Bullets/bullet_player.png");
        }
        pool = GetComponentInParent<ObjectPool>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = transform.up * speed;
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
