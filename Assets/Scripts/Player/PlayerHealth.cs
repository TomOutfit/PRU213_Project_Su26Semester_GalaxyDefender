using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 100;
    public int maxShield = 50;

    public int currentHP { get; private set; }
    public int currentShield { get; private set; }

    [HideInInspector]
    public bool isDashing = false;
    [HideInInspector]
    public bool isInvincible = false;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHPChanged;
    public UnityEvent<int> OnShieldChanged;
    public UnityEvent OnDamageTaken;

    private void Start()
    {
        currentHP = maxHP;
        currentShield = 0; // Shield only restores via Shield power-up
        
        OnHPChanged?.Invoke(currentHP);
        OnShieldChanged?.Invoke(currentShield);
    }

    public void TakeDamage(int damage)
    {
        if (isDashing || isInvincible) return;

        OnDamageTaken?.Invoke();
        ScoreManager.Instance?.OnPlayerDamaged();

        // Shield absorbs first
        if (currentShield > 0)
        {
            if (damage > currentShield)
            {
                damage -= currentShield;
                currentShield = 0;
            }
            else
            {
                currentShield -= damage;
                damage = 0;
            }
            OnShieldChanged?.Invoke(currentShield);
        }

        // Remaining damage hits HP
        if (damage > 0)
        {
            currentHP -= damage;
            if (currentHP < 0) currentHP = 0;
            OnHPChanged?.Invoke(currentHP);

            if (currentHP <= 0)
            {
                Die();
            }
        }
    }

    public void AddHealth(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP);
    }

    public void AddShield(int amount)
    {
        currentShield = Mathf.Min(maxShield, currentShield + amount);
        OnShieldChanged?.Invoke(currentShield);
    }

    public void ResetHealthAndShield()
    {
        currentHP = maxHP;
        currentShield = 0;
        OnHPChanged?.Invoke(currentHP);
        OnShieldChanged?.Invoke(currentShield);
    }

    private void Die()
    {
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
