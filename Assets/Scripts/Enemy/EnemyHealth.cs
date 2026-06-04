using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 20;
    public int scorePoints = 100;

    public int currentHP { get; private set; }

    [Header("Events")]
    public UnityEvent OnDeath;

    private ObjectPool hitEffectPool;
    private ObjectPool explosionPool;

    private void OnEnable()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        // Spawn hit effect from pool
        if (hitEffectPool == null)
        {
            hitEffectPool = GameObject.Find("HitEffectPool")?.GetComponent<ObjectPool>();
        }
        if (hitEffectPool != null)
        {
            hitEffectPool.Get(transform.position, Quaternion.identity);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();

        // Increment score and combo
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scorePoints);
            ScoreManager.Instance.OnEnemyKilled();
        }

        // Play SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("sfx_explosion_small");
        }

        // Spawn explosion effect
        if (explosionPool == null)
        {
            explosionPool = GameObject.Find("ExplosionSmallPool")?.GetComponent<ObjectPool>();
        }
        if (explosionPool != null)
        {
            explosionPool.Get(transform.position, Quaternion.identity);
        }

        // 30% chance to drop powerup
        if (Random.value < 0.3f && PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Drop(transform.position);
        }

        // Release enemy to pool
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ReleaseEnemy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
