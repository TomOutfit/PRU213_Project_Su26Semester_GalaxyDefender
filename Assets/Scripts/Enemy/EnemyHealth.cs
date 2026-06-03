using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 30;
    public int points = 100;
    
    private int currentHP;

    [Header("Events")]
    public UnityEvent OnDeath;

    private void OnEnable()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return; // Already dead

        currentHP -= damage;
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(points);
            ScoreManager.Instance.OnEnemyKilled();
        }

        if (PowerUpManager.Instance != null)
        {
            // 30% chance to drop power-up
            if (Random.value <= 0.3f)
            {
                PowerUpManager.Instance.Drop(transform.position);
            }
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
