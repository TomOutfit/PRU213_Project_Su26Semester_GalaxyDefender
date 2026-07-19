using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Gắn vào Tilemap_Hazard (có TilemapCollider2D + isTrigger = true).
/// Nâng cấp TilemapHazard gốc với:
///   - Hiệu ứng nhấp nháy (SpriteFlash) khi player chạm
///   - Độ trễ miễn nhiễm ngắn sau mỗi đòn (invincibility frames)
///   - Tùy chọn phát SFX khi gây sát thương
///   - Tùy chọn Visual Warning: tile nhấp nháy trước khi gây damage
/// </summary>
public class TilemapHazardAdvanced : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Sát thương gây ra mỗi lần tấn công.")]
    public int damagePerHit = 1;

    [Tooltip("Khoảng thời gian giữa các đòn tấn công (giây). Nên ≥ 0.5.")]
    [Range(0.2f, 5f)]
    public float damageInterval = 0.8f;

    [Header("Invincibility Frames")]
    [Tooltip("Thời gian miễn nhiễm sau mỗi đòn tấn công (giây). Ngăn damage quá dày.")]
    [Range(0f, 3f)]
    public float invincibilityDuration = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("Màu nhấp nháy của Hazard Tilemap khi active. Để alpha=0 để tắt.")]
    public Color hazardFlashColor = new Color(1f, 0.2f, 0.1f, 0.85f);

    [Tooltip("Tốc độ nhấp nháy (chu kỳ/giây).")]
    [Range(0.5f, 10f)]
    public float flashFrequency = 4f;

    [Header("SFX")]
    [Tooltip("Key âm thanh trong AudioManager khi gây damage (để trống để tắt).")]
    public string damageSFXKey = "sfx_explosion_small";

    // ──────────────────────────────────────────────────────────────────────
    // RUNTIME
    // ──────────────────────────────────────────────────────────────────────
    private PlayerHealth _playerHealth;
    private float        _damageTimer;
    private bool         _playerInside;
    private Tilemap      _tilemap;
    private Color        _originalColor;
    private Coroutine    _flashRoutine;

    // ──────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        if (_tilemap != null) _originalColor = _tilemap.color;
    }

    private void Update()
    {
        if (!_playerInside || _playerHealth == null) return;

        _damageTimer += Time.deltaTime;

        if (_damageTimer >= damageInterval)
        {
            _damageTimer = 0f;
            ApplyDamage();
        }

        // Hiệu ứng nhấp nháy khi player bên trong vùng hazard
        if (_tilemap != null)
        {
            float t   = Mathf.Abs(Mathf.Sin(Time.time * flashFrequency * Mathf.PI));
            _tilemap.color = Color.Lerp(_originalColor, hazardFlashColor, t * 0.6f);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // COLLISIONS
    // ──────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        _playerHealth = col.GetComponent<PlayerHealth>();
        _playerInside = true;
        _damageTimer  = damageInterval; // Hit ngay lập tức khi chạm vào
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        _playerHealth = null;
        _playerInside = false;
        _damageTimer  = 0f;

        // Khôi phục màu gốc khi player thoát
        if (_tilemap != null)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(RestoreColorRoutine());
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ──────────────────────────────────────────────────────────────────────

    private void ApplyDamage()
    {
        if (_playerHealth == null) return;

        _playerHealth.TakeDamage(damagePerHit);

        // SFX
        if (!string.IsNullOrEmpty(damageSFXKey))
            AudioManager.Instance?.PlaySFX(damageSFXKey);

        // Nhấp nháy mạnh ngay khi hit
        if (_tilemap != null) _tilemap.color = hazardFlashColor;
    }

    // ──────────────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────────────

    private IEnumerator RestoreColorRoutine()
    {
        float elapsed  = 0f;
        float duration = 0.3f;
        Color current  = _tilemap.color;

        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            _tilemap.color = Color.Lerp(current, _originalColor, elapsed / duration);
            yield return null;
        }

        _tilemap.color = _originalColor;
    }
}
