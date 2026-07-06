using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Shared health component for all enemies (drone, hunter, boss). Tracks HP, flashes on hit, and
/// on death awards <see cref="points"/> to the score, rolls a power-up drop, plays explosion
/// VFX/SFX, and returns the enemy to its pool. Damage is ignored while a boss is spawning/dying.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 50;
    public int points = 300;
    
    private int currentHP;
    public int CurrentHP => currentHP;
    private bool hasDied = false;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHealthChanged;

    private void OnEnable()
    {
        currentHP = maxHP;
        hasDied = false;
    }

    /// <summary>Subtracts HP, flashes the sprite, and triggers death when HP reaches zero.</summary>
    public void TakeDamage(int damage)
    {
        if (currentHP <= 0 || hasDied) return; // Already dead

        BossController boss = GetComponent<BossController>();
        if (boss != null && (boss.isSpawning || boss.isDying)) return;

        currentHP -= damage;
        OnHealthChanged?.Invoke(currentHP);
        GetComponent<SpriteFlash>()?.Flash(0.08f, Color.white);

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    /// <summary>Awards score, rolls a power-up drop, plays explosion VFX/SFX, and pools the enemy.</summary>
    private void Die()
    {
        hasDied = true;

        OnDeath?.Invoke();

        if (GetComponent<BossController>() != null)
        {
            return;
        }

        WaveManager.Instance?.OnEnemyDestroyed(gameObject);

        CameraShake.Instance?.Shake(0.15f, 0.1f);

        // Play SFX
        AudioManager.Instance?.PlaySFX("sfx_explosion_small");

        // Spawn small explosion
        GameObject poolObj = GameObject.Find("ExplosionSmallPool");
        if (poolObj != null)
        {
            ObjectPool explosionPool = poolObj.GetComponent<ObjectPool>();
            if (explosionPool != null)
            {
                explosionPool.Get(transform.position, Quaternion.identity);
            }
        }

        // Spawn large explosion overlay for visual impact
        GameObject largePoolObj = GameObject.Find("ExplosionLargePool");
        if (largePoolObj != null)
        {
            ObjectPool largeExplosionPool = largePoolObj.GetComponent<ObjectPool>();
            if (largeExplosionPool != null)
            {
                largeExplosionPool.Get(transform.position, Quaternion.identity);
            }
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(points);
            ScoreManager.Instance.OnEnemyKilled();
        }

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Drop(transform.position);
        }

        ObjectPool pool = GetComponentInParent<ObjectPool>();
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
