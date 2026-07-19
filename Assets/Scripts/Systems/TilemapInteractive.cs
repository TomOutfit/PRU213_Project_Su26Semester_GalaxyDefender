using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Quản lý hành vi động cho các tile đặc biệt:
///   - TRIGGER tiles: Khi player chạm vào → bật/tắt một tilemap khác (cửa, nền ẩn...)
///   - BREAKABLE tiles: Tile bị phá hủy khi trúng đạn player
///   - MOVING platform tiles: Tilemap di chuyển theo pattern sin/cos
///
/// Gắn vào Tilemap cần có hành vi đặc biệt trong scene Level.
/// </summary>
public class TilemapInteractive : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────────
    // CONFIG
    // ──────────────────────────────────────────────────────────────────────

    public enum TileInteractionType
    {
        Trigger,        // Kích hoạt/tắt một tilemap khác khi player chạm
        Breakable,      // Tile bị xóa khi trúng đạn
        MovingPlatform, // Tilemap tự di chuyển qua lại
        None
    }

    [Header("Loại tương tác")]
    public TileInteractionType interactionType = TileInteractionType.None;

    // ── TRIGGER ───────────────────────────────────────────────────────────
    [Header("Trigger Settings (khi type = Trigger)")]
    [Tooltip("Tilemap cần bật/tắt khi player chạm vào tilemap này.")]
    public Tilemap targetTilemap;
    [Tooltip("Bật (true) hoặc tắt (false) targetTilemap khi trigger.")]
    public bool activateOnTrigger = true;
    [Tooltip("SFX khi trigger (để trống để bỏ qua).")]
    public string triggerSFXKey = "sfx_shoot_player";

    // ── MOVING PLATFORM ───────────────────────────────────────────────────
    [Header("Moving Platform Settings (khi type = MovingPlatform)")]
    [Tooltip("Biên độ di chuyển ngang (world units).")]
    public float moveAmplitudeX = 2f;
    [Tooltip("Biên độ di chuyển dọc (world units). Thường = 0 cho space shooter.")]
    public float moveAmplitudeY = 0f;
    [Tooltip("Tốc độ dao động (Hz).")]
    [Range(0.1f, 5f)]
    public float moveFrequency = 0.5f;

    // ── BREAKABLE ─────────────────────────────────────────────────────────
    [Header("Breakable Settings (khi type = Breakable)")]
    [Tooltip("Số lần trúng đạn trước khi tile bị xóa (0 = tắt tính năng này).")]
    public int hitsToBreak = 3;
    [Tooltip("SFX khi tile bị phá (để trống để bỏ qua).")]
    public string breakSFXKey = "sfx_explosion_small";

    // ──────────────────────────────────────────────────────────────────────
    // RUNTIME
    // ──────────────────────────────────────────────────────────────────────

    private Tilemap      _tilemap;
    private Vector3      _startPosition;
    private int          _hitCount;
    private bool         _triggered;
    private Color        _originalColor;

    // ──────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _tilemap       = GetComponent<Tilemap>();
        _startPosition = transform.position;
        if (_tilemap != null) _originalColor = _tilemap.color;
    }

    private void Update()
    {
        if (interactionType == TileInteractionType.MovingPlatform)
        {
            UpdateMovingPlatform();
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // TRIGGER
    // ──────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (interactionType != TileInteractionType.Trigger) return;
        if (!col.CompareTag("Player")) return;
        if (_triggered) return;

        _triggered = true;

        if (targetTilemap != null)
        {
            bool newState = activateOnTrigger;
            targetTilemap.gameObject.SetActive(newState);
            Debug.Log($"[TilemapInteractive] Trigger → {targetTilemap.name} SetActive({newState})");
        }

        if (!string.IsNullOrEmpty(triggerSFXKey))
            AudioManager.Instance?.PlaySFX(triggerSFXKey);

        // Flash xác nhận trigger
        StartCoroutine(TriggerFlash());
    }

    private IEnumerator TriggerFlash()
    {
        if (_tilemap == null) yield break;
        Color flash = new Color(0f, 1f, 0.5f, 1f); // Neon green xác nhận
        float t = 0f;
        while (t < 0.4f)
        {
            t            += Time.deltaTime;
            _tilemap.color = Color.Lerp(flash, _originalColor, t / 0.4f);
            yield return null;
        }
        _tilemap.color = _originalColor;
    }

    // ──────────────────────────────────────────────────────────────────────
    // BREAKABLE (gọi từ bên ngoài — ví dụ: BulletController khi va chạm)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi đạn player bắn trúng tilemap này.
    /// Trả về true nếu tile đã bị phá hủy hoàn toàn.
    /// </summary>
    public bool RegisterHit(Vector3Int cellPosition)
    {
        if (interactionType != TileInteractionType.Breakable) return false;
        if (_tilemap == null) return false;

        _hitCount++;

        // Nhấp nháy khi trúng
        StartCoroutine(HitFlash(cellPosition));

        if (_hitCount >= hitsToBreak)
        {
            // Xóa tile tại vị trí bị bắn trúng
            _tilemap.SetTile(cellPosition, null);

            if (!string.IsNullOrEmpty(breakSFXKey))
                AudioManager.Instance?.PlaySFX(breakSFXKey);

            _hitCount = 0; // Reset cho lần sau
            return true;
        }

        return false;
    }

    private IEnumerator HitFlash(Vector3Int cell)
    {
        if (_tilemap == null) yield break;
        Color hitColor = new Color(1f, 0.5f, 0f, 1f); // Orange khi trúng
        _tilemap.color = hitColor;
        yield return new WaitForSeconds(0.08f);
        _tilemap.color = _originalColor;
    }

    // ──────────────────────────────────────────────────────────────────────
    // MOVING PLATFORM
    // ──────────────────────────────────────────────────────────────────────

    private void UpdateMovingPlatform()
    {
        if (GameManager.Instance?.CurrentState == GameManager.State.Paused) return;

        float t  = Time.time * moveFrequency * Mathf.PI * 2f;
        float dx = Mathf.Sin(t) * moveAmplitudeX;
        float dy = Mathf.Cos(t) * moveAmplitudeY;

        transform.position = _startPosition + new Vector3(dx, dy, 0f);
    }

    // ──────────────────────────────────────────────────────────────────────
    // RESET (dùng khi restart level)
    // ──────────────────────────────────────────────────────────────────────

    public void ResetState()
    {
        _triggered  = false;
        _hitCount   = 0;
        transform.position = _startPosition;
        if (_tilemap != null) _tilemap.color = _originalColor;
    }
}
