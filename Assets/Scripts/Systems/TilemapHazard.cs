using UnityEngine;

/// <summary>
/// Attach to any Tilemap_Hazard GameObject (TilemapCollider2D set to isTrigger).
/// Deals damage to the player while they stand on the hazard tile.
/// </summary>
public class TilemapHazard : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage dealt per second.")]
    public float damagePerSecond = 10000f;

    [Tooltip("Interval (in seconds) between damage applications to avoid playing hit effects every frame.")]
    public float damageInterval = 0.5f;

    private float damageTimer = 0f;
    private PlayerHealth playerHealth = null;

    private void Update()
    {
        if (playerHealth != null)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                int damageToDeal = Mathf.RoundToInt(damagePerSecond * damageInterval);
                if (damageToDeal > 0)
                {
                    playerHealth.TakeDamage(damageToDeal);
                }
                damageTimer = 0f;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerHealth = collision.GetComponent<PlayerHealth>();
            // Set timer to damageInterval so the first hit lands immediately on contact
            damageTimer = damageInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerHealth = null;
            damageTimer = 0f;
        }
    }
}
