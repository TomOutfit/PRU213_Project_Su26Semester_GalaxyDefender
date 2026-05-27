using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
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
