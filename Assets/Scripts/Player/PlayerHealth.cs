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

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHPChanged;
    public UnityEvent<int> OnShieldChanged;
    public UnityEvent OnDamageTaken;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        currentHP = maxHP;
        currentShield = 0; // Shield only restores via Shield power-up
        
        OnHPChanged?.Invoke(currentHP);
        OnShieldChanged?.Invoke(currentShield);
    }

    public void TakeDamage(int damage)
    {
        if (isDashing) return;

        OnDamageTaken?.Invoke();

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        CameraShake.Instance?.Shake(0.2f, 0.15f);
        GetComponent<SpriteFlash>()?.Flash(0.1f, Color.red);

        // Play SFX
        AudioManager.Instance?.PlaySFX("sfx_player_hit");

        // Spawn hit effect
        GameObject poolObj = GameObject.Find("HitEffectPool");
        if (poolObj != null)
        {
            ObjectPool pool = poolObj.GetComponent<ObjectPool>();
            if (pool != null)
            {
                pool.Get(transform.position, Quaternion.identity);
            }
        }

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

    private void Die()
    {
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
