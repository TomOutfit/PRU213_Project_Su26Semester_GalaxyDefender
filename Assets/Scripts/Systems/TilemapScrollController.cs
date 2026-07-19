using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Cuộn toàn bộ Tilemap layers xuống dưới theo từng tốc độ riêng.
/// Khi một layer trôi ra khỏi viewport phía dưới → reset lên trên,
/// tạo hiệu ứng level vô hạn / seamless looping.
///
/// FIX BUILD: tileHeight được tính lazy ở Update() đầu tiên (không phải Start())
/// vì khi được AddComponent() từ LevelTilemapSpawner.Awake(), Start() chạy
/// trước khi Unity recalculate tilemap bounds → bounds.size = zero → tileHeight = 0.
/// </summary>
public class TilemapScrollController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────────
    // INSPECTOR CONFIG
    // ──────────────────────────────────────────────────────────────────────

    [System.Serializable]
    public class TilemapLayer
    {
        [Tooltip("Tilemap GameObject (phải có Tilemap component)")]
        public Tilemap tilemap;

        [Tooltip("Tốc độ cuộn dọc (đơn vị/giây). BG chậm, Collision/Hazard nhanh hơn.")]
        [Range(0.5f, 20f)]
        public float scrollSpeed = 3f;

        [Tooltip("Có loop (reset lên trên) không? Tắt nếu là tilemap 1 lần duy nhất.")]
        public bool loop = true;

        // Runtime — khởi tạo trong InitLayers(), không phải Start()
        [HideInInspector] public float   tileHeight;  // chiều cao tilemap (world units)
        [HideInInspector] public Vector3 startPos;    // vị trí gốc khi scene load
        [HideInInspector] public bool    initialized; // đã tính bounds chưa
    }

    [Header("Tilemap Layers (Kéo thả theo thứ tự: BG → Decor → Collision → Hazard)")]
    public List<TilemapLayer> layers = new List<TilemapLayer>();

    [Header("Scroll Speed Multiplier (nhân toàn bộ — điều chỉnh khi cần)")]
    [Range(0.1f, 5f)]
    public float globalSpeedMultiplier = 1f;

    [Header("Pause khi game không ở trạng thái Playing")]
    public bool pauseWhenNotPlaying = true;

    private bool _allInitialized = false;

    // ──────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi thủ công từ LevelTilemapSpawner sau khi PaintAllLayers() hoàn tất.
    /// Cũng được tự động gọi lại ở Update() đầu tiên nếu chưa init.
    /// </summary>
    public void InitLayers()
    {
        bool allReady = true;

        foreach (var layer in layers)
        {
            if (layer.tilemap == null) continue;

            if (!layer.initialized)
            {
                // CompressBounds() bắt Unity recalculate bounds ngay lập tức
                layer.tilemap.CompressBounds();

                Bounds bounds = layer.tilemap.localBounds;
                float  height = bounds.size.y * layer.tilemap.transform.lossyScale.y;

                if (height > 0f)
                {
                    layer.tileHeight   = height;
                    layer.startPos     = layer.tilemap.transform.position;
                    layer.initialized  = true;
                }
                else
                {
                    // Bounds chưa ready — thử lại frame sau
                    allReady = false;
                }
            }
        }

        _allInitialized = allReady;

        if (_allInitialized)
        {
            Debug.Log("[TilemapScrollController] ✅ Tất cả layers đã init bounds.");
        }
    }

    private void Start()
    {
        // Cố gắng init ngay — nhưng nếu bounds chưa có thì sẽ retry ở Update
        InitLayers();
    }

    private void Update()
    {
        // Retry init nếu Start() chưa tính được bounds (vì tilemap vừa được tạo runtime)
        if (!_allInitialized)
        {
            InitLayers();
            if (!_allInitialized) return; // Chờ frame tiếp theo
        }

        // Dừng cuộn nếu game đang pause/gameover
        if (pauseWhenNotPlaying && GameManager.Instance != null)
        {
            var state = GameManager.Instance.CurrentState;
            if (state == GameManager.State.Paused  ||
                state == GameManager.State.GameOver ||
                state == GameManager.State.Victory)
                return;
        }

        float dt = Time.deltaTime;

        foreach (var layer in layers)
        {
            if (layer.tilemap == null || !layer.initialized) continue;

            // Di chuyển layer xuống dưới
            float move = layer.scrollSpeed * globalSpeedMultiplier * dt;
            layer.tilemap.transform.position += Vector3.down * move;

            // Loop: nếu trôi quá phía dưới → nhảy về trên
            if (layer.loop && layer.tileHeight > 0f)
            {
                float yPos = layer.tilemap.transform.position.y;
                if (yPos <= layer.startPos.y - layer.tileHeight)
                {
                    layer.tilemap.transform.position = new Vector3(
                        layer.tilemap.transform.position.x,
                        layer.startPos.y,
                        layer.tilemap.transform.position.z
                    );
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Tạm dừng/tiếp tục tất cả layer scroll.</summary>
    public void SetPaused(bool paused) => pauseWhenNotPlaying = paused;

    /// <summary>Đặt lại tất cả layer về vị trí gốc (dùng khi restart level).</summary>
    public void ResetAllLayers()
    {
        foreach (var layer in layers)
        {
            if (layer.tilemap != null)
                layer.tilemap.transform.position = layer.startPos;
        }
    }

    /// <summary>Tăng tốc độ cuộn toàn bộ (dùng khi lên wave khó hơn).</summary>
    public void SetGlobalSpeed(float multiplier)
    {
        globalSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 10f);
    }
}
