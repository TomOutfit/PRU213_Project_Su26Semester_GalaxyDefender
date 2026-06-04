using UnityEngine;

public class TilemapHazard : MonoBehaviour
{
    private float damageAccumulator = 0f;
    public float damagePerSecond = 10f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                damageAccumulator += damagePerSecond * Time.deltaTime;
                if (damageAccumulator >= 1f)
                {
                    int deal = Mathf.FloorToInt(damageAccumulator);
                    playerHealth.TakeDamage(deal);
                    damageAccumulator -= deal;
                }
            }
        }
    }
}
