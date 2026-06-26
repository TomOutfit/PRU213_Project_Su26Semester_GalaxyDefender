using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    protected virtual void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            #if UNITY_2023_1_OR_NEWER
            rb.linearVelocity = Vector2.down * 1.5f;
            #else
            rb.velocity = Vector2.down * 1.5f;
            #endif
        }
    }

    protected virtual void Update()
    {
        // Fallback destruction if it goes too far down without being seen
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnBecameInvisible()
    {
        // Only destroy if it was already seen and then left the screen
        // or just rely on Update boundary check to be safer
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        ApplyEffect(ph);
        ScoreManager.Instance?.AddScore(50);
        Destroy(gameObject);
    }

    public abstract void ApplyEffect(PlayerHealth ph);
}
