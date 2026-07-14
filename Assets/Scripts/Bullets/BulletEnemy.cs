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
    public int damage = 8000;
    
    private Rigidbody2D rb;
    private ObjectPool pool;
    private TrailRenderer trail;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Add a neon-trail for artistic enemy bullets
        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.0f;
            trail.sortingOrder = 4;
            
            // Bright neon red/purple trail gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.2f, 0.2f, 1f), 0f), 
                    new GradientColorKey(new Color(0.8f, 0f, 0.8f, 0.3f), 1f) 
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
        damage = 8000;
        pool = GetComponentInParent<ObjectPool>();
    }

    private void OnEnable()
    {
        if (trail != null)
        {
            trail.Clear();
        }
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
