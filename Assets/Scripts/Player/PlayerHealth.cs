using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks the player's HP and shield and broadcasts changes to the HUD. Shield absorbs damage
/// before HP. Damage taken while dashing is ignored (dash i-frames). Raises OnDeath when HP
/// reaches zero. Health/shield are restored only via power-ups.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 1000000;
    public int maxShield = 1000000;

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

    private void Awake()
    {
        maxHP = 1000000;
        maxShield = 1000000;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        currentHP = maxHP;
        currentShield = maxShield; // Starts with a full shield!
        
        OnHPChanged?.Invoke(currentHP);
        OnShieldChanged?.Invoke(currentShield);
    }

    /// <summary>
    /// Applies damage and also knocks the player back away from the damage source. Skips the
    /// knockback while dashing (i-frames).
    /// </summary>
    /// <param name="damage">Raw damage before shield absorption.</param>
    /// <param name="hitSourcePos">World position of the bullet/source that caused the hit.</param>
    public void TakeDamage(int damage, Vector2 hitSourcePos)
    {
        if (!isDashing)
        {
            Vector2 dir = (Vector2)transform.position - hitSourcePos;
            GetComponent<PlayerController>()?.Knockback(dir);
        }
        TakeDamage(damage);
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

    /// <summary>Restores HP up to <see cref="maxHP"/> (used by the health power-up).</summary>
    public void AddHealth(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP);
    }

    /// <summary>Restores shield up to <see cref="maxShield"/> (used by the shield power-up).</summary>
    public void AddShield(int amount)
    {
        currentShield = Mathf.Min(maxShield, currentShield + amount);
        OnShieldChanged?.Invoke(currentShield);
    }

    private void Die()
    {
        OnDeath?.Invoke();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseLife();
            
            if (GameManager.Instance.GetCurrentLives() > 0)
            {
                // Respawn sequence
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        // Hide and disable
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        isDashing = true; // i-frames during respawn

        Time.timeScale = 0.3f; // Enter slow-motion for dramatic death

        yield return new WaitForSecondsRealtime(0.8f);

        Time.timeScale = 1.0f; // Restore normal speed

        // Reset position to bottom center
        transform.position = new Vector3(0, -4f, 0);
        currentHP = maxHP;
        currentShield = 0;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.ResetWeapon();
        
        OnHPChanged?.Invoke(currentHP);
        OnShieldChanged?.Invoke(currentShield);

        // Show and enable with flash
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<SpriteFlash>()?.Flash(1.5f, Color.white);
        
        yield return new WaitForSeconds(1.5f);
        
        GetComponent<Collider2D>().enabled = true;
        isDashing = false;
    }
}
