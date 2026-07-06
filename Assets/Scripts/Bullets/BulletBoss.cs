using UnityEngine;

/// <summary>
/// Boss projectile. Supports per-phase sprites and damage values via SetPhase().
/// Direction is set externally by BossController via SetDirection().
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BulletBoss : MonoBehaviour
{
    [Header("Phase 1 — Single aimed shot")]
    public float phase1Speed = 6f;
    public int phase1Damage = 15;

    [Header("Phase 2 — Faster, stronger")]
    public float phase2Speed = 8f;
    public int phase2Damage = 20;

    [Header("Phase 3 — Fastest, hardest")]
    public float phase3Speed = 10f;
    public int phase3Damage = 30;

    [Header("Sprites")]
    public string spritePhase1 = "Assets/Sprites/Bullets/bullet_boss_phase1.png";
    public string spritePhase2 = "Assets/Sprites/Bullets/bullet_boss_phase2.png";
    public string spritePhase3 = "Assets/Sprites/Bullets/bullet_boss_phase3.png";

    [Header("Runtime")]
    [HideInInspector] public int currentPhase = 1;

    private Rigidbody2D rb;
    private ObjectPool pool;
    private Vector2 direction = Vector2.down;
    private float activeSpeed;
    private int activeDamage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ApplyPhase(1);
        pool = GetComponentInParent<ObjectPool>();
    }

    /// <summary>Call this before or after enabling the bullet to set phase (1-3).</summary>
    public void SetPhase(int phase)
    {
        currentPhase = Mathf.Clamp(phase, 1, 3);
        ApplyPhase(currentPhase);
    }

    /// <summary>Force override damage and speed for a specific phase — used by BossController.</summary>
    public void SetPhaseDamageAndSpeed(int phase, int damage, float speed)
    {
        currentPhase = Mathf.Clamp(phase, 1, 3);
        activeDamage = damage;
        activeSpeed = speed;
        string path = phase switch
        {
            2 => spritePhase2,
            3 => spritePhase3,
            _ => spritePhase1
        };
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) RuntimeSpriteFixer.EnsureSprite(sr, path, true);
    }

    private void ApplyPhase(int phase)
    {
        activeSpeed = phase switch
        {
            2 => phase2Speed,
            3 => phase3Speed,
            _ => phase1Speed
        };

        activeDamage = phase switch
        {
            2 => phase2Damage,
            3 => phase3Damage,
            _ => phase1Damage
        };

        string path = phase switch
        {
            2 => spritePhase2,
            3 => spritePhase3,
            _ => spritePhase1
        };

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            RuntimeSpriteFixer.EnsureSprite(sr, path, true);
        }
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        transform.up = direction;
    }

    private void FixedUpdate()
    {
        #if UNITY_2023_1_OR_NEWER
        rb.linearVelocity = direction.normalized * activeSpeed;
        #else
        rb.velocity = direction.normalized * activeSpeed;
        #endif
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            health?.TakeDamage(activeDamage, transform.position);
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
            pool.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
}
