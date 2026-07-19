using System.Collections;
using UnityEngine;

/// <summary>
/// Space Mine hazard obstacle. Moves downward, plays pulsing animation,
/// deals contact damage to player on collision, and explodes when shot/destroyed.
/// Uses dynamic pooling for optimal performance.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class ObstacleMine : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;

    [Header("Damage")]
    public int contactDamage = 15;

    private Rigidbody2D rb;
    private ObjectPool minePool;
    private bool hasBeenVisible = false;
    private bool _dying = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Find parent pool if instantiated via ObjectPool
        minePool = GetComponentInParent<ObjectPool>();

        // Programmatically guarantee points are a clean multiple of 1,000
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.points = 5000;
        }
    }

    private void OnEnable()
    {
        hasBeenVisible = false;
        _dying = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Dynamically ensure sprite is correct and fallback is loaded if needed
            RuntimeSpriteFixer.EnsureSprite(sr, "Assets/Sprites/Obstacles/obstacle_mine_sheet.png", true);
        }
    }

    private void FixedUpdate()
    {
        // Drift straight down
        rb.MovePosition(rb.position + Vector2.down * moveSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(contactDamage);
            }

            // Explode immediately on contact
            EnemyHealth health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(9999); // Force instant explosion
            }
        }
    }

    private void OnBecameVisible()
    {
        hasBeenVisible = true;
    }

    private void OnBecameInvisible()
    {
        if (!hasBeenVisible) return;
        if (_dying) return;

        _dying = true;
        WaveManager.Instance?.OnEnemyDestroyed(gameObject);

        if (minePool != null)
        {
            minePool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
