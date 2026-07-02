using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    private const float BASE_SCALE = 1.6f;
    private const float PULSE_AMPLITUDE = 0.1f;
    private const float ROTATE_SPEED = 45f;

    // Magnet: distance at which power-ups are attracted to player
    public static float MagnetRadiusMultiplier = 1.0f;
    private const float MAGNET_RADIUS = 2.5f;
    private const float MAGNET_SPEED = 5f;

    private float _pulseTimer;

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
        // Pulsing scale animation
        _pulseTimer += Time.deltaTime;
        float scale = BASE_SCALE + Mathf.Sin(_pulseTimer * 3f) * PULSE_AMPLITUDE;
        transform.localScale = new Vector3(scale, scale, 1f);

        // Slow rotation for the glow ring child
        var glow = transform.Find("GlowRing");
        if (glow != null)
        {
            glow.Rotate(0f, 0f, -ROTATE_SPEED * Time.deltaTime);
        }

        // Magnet effect: pull toward player when in range
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 dir = player.transform.position - transform.position;
            float dist = dir.magnitude;
            float effectiveRadius = MAGNET_RADIUS * MagnetRadiusMultiplier;
            if (dist < effectiveRadius && dist > 0.1f)
            {
                // Scale pull strength by proximity (stronger when closer)
                float pull = (1f - dist / effectiveRadius) * MAGNET_SPEED;
                transform.position += dir.normalized * pull * Time.deltaTime;

                // Disable rigidbody velocity while being attracted
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    #if UNITY_2023_1_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
                    #else
                    rb.velocity = Vector2.zero;
                    #endif
                }
            }
        }

        // Fallback destruction if it goes too far down without being seen
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnBecameInvisible()
    {
    }

    protected virtual int ScoreValue => 50;

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        ApplyEffect(ph);
        AudioManager.Instance?.PlaySFX(SFXKey);
        ScoreManager.Instance?.AddScore(ScoreValue);
        Destroy(gameObject);
    }

    public abstract void ApplyEffect(PlayerHealth ph);
    protected abstract string SFXKey { get; }
}
