using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tạo 5 tilemap layers (DeepSpace, Station, Decor, Asteroid, Hazard) tại runtime.
/// Mỗi layer có Grid riêng để scroll và loop độc lập.
/// Grid được đặt đúng vị trí dựa trên camera viewport.
/// </summary>
public class LevelTilemapSpawner : MonoBehaviour
{
    [Header("Sprite Data")]
    public TileSetData tileSetData;

    [Header("Map Size")]
    public int mapWidth  = 32;   // đủ rộng hơn viewport (camera halfW ~14 ở 16:9)
    public int mapHeight = 50;   // đủ cao để scroll (>= 3× viewport height)

    [Header("Scroll Speed (units/s)")]
    public float speedDeepSpace = 0.8f;
    public float speedStation   = 2.0f;
    public float speedDecor     = 2.5f;
    public float speedAsteroid  = 3.5f;
    public float speedHazard    = 3.0f;
    public float speedGround    = 2.0f;
    public float speedPlatform  = 2.2f;

    [Header("Level")]
    [Range(1, 3)]
    public int levelIndex = 1;

    [Header("Seed (0 = auto)")]
    public int randomSeed = 0;

    // ── runtime ──────────────────────────────────────────────────────────
    // Mỗi layer có Grid + Tilemap riêng
    private struct LayerData
    {
        public GameObject  gridRoot;
        public Tilemap     tilemap;
        public float       scrollSpeed;
        public float       tileHeight;
        public Vector3     startPos;
        public bool        initialized;
    }

    private LayerData[] _layers;

    // Strong references — tránh GC collect tile instances trong build
    private readonly List<Tile>                  _tilePool  = new List<Tile>(1024);
    private readonly Dictionary<Sprite, Tile>    _tileCache = new Dictionary<Sprite, Tile>();

    // Camera info (đọc 1 lần trong Awake)
    private float _camStartX;
    private float _camStartY;

    // ─────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (tileSetData == null)
            tileSetData = Resources.Load<TileSetData>("TileSetData");

        if (tileSetData == null || !tileSetData.IsValid())
        {
            Debug.LogError("[LevelTilemapSpawner] TileSetData missing! " +
                           "Run: Galaxy Defender → Build TileSetData Asset");
            return;
        }

        // Lấy thông tin camera để đặt Grid đúng vị trí
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[LevelTilemapSpawner] No Main Camera found!");
            return;
        }

        float camHalfW = cam.orthographicSize * cam.aspect;
        float camHalfH = cam.orthographicSize;
        _camStartX = cam.transform.position.x - camHalfW;
        _camStartY = cam.transform.position.y - camHalfH - 1f; // 1 tile dưới đáy viewport

        // Điều chỉnh mapWidth để luôn cover toàn bộ viewport
        int minWidth = Mathf.CeilToInt(camHalfW * 2f) + 4;
        if (mapWidth < minWidth) mapWidth = minWidth;

        int seed = randomSeed != 0 ? randomSeed : System.DateTime.Now.Millisecond + levelIndex * 1000;
        Random.InitState(seed);

        BuildLayers();

        // Lưu base speeds cho SetDifficultyScale
        _baseSpeeds = new float[_layers.Length];
        for (int i = 0; i < _layers.Length; i++)
            _baseSpeeds[i] = _layers[i].scrollSpeed;

        SetupHazard();
        SetupMovingPlatforms();

        Debug.Log($"[LevelTilemapSpawner] ✅ Level {levelIndex} built. " +
                  $"Grid origin=({_camStartX:F1},{_camStartY:F1}), " +
                  $"map={mapWidth}×{mapHeight}, tiles={_tilePool.Count}");
    }

    private void Update()
    {
        // Pause khi cần
        if (GameManager.Instance != null)
        {
            var s = GameManager.Instance.CurrentState;
            if (s == GameManager.State.Paused ||
                s == GameManager.State.GameOver ||
                s == GameManager.State.Victory)
                return;
        }

        for (int i = 0; i < _layers.Length; i++)
        {
            ref LayerData layer = ref _layers[i];
            if (layer.gridRoot == null) continue;

            // Init bounds lazy nếu chưa xong
            if (!layer.initialized)
            {
                layer.tilemap.CompressBounds();
                float h = layer.tilemap.localBounds.size.y;
                if (h > 0f)
                {
                    layer.tileHeight  = h;
                    layer.startPos    = layer.gridRoot.transform.position;
                    layer.initialized = true;
                }
                continue;
            }

            // Scroll xuống
            float move = layer.scrollSpeed * Time.deltaTime;
            layer.gridRoot.transform.position += Vector3.down * move;

            // Loop: khi lọt hoàn toàn khỏi viewport dưới → nhảy lên trên
            if (layer.tileHeight > 0f)
            {
                float y = layer.gridRoot.transform.position.y;
                if (y <= layer.startPos.y - layer.tileHeight)
                {
                    layer.gridRoot.transform.position = new Vector3(
                        layer.gridRoot.transform.position.x,
                        layer.startPos.y,
                        layer.gridRoot.transform.position.z);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BUILD
    // ─────────────────────────────────────────────────────────────────────

    private void BuildLayers()
    {
        _layers = new LayerData[7];
        // Sorting order scheme (phối hợp với ParallaxBG và Tilemap tĩnh trong scene):
        // -10,-9,-8 : ParallaxBG sprites (background xa → gần)
        //  -7       : Tilemap_BG static (scene)
        //  -6       : Runtime DeepSpace  ← fill gap giữa BG và gameplay
        //  -5       : (dự phòng)
        //  -4       : Runtime Station
        //  -3       : Runtime Decor
        //  -2       : Tilemap_Collision static (scene)
        //  -1       : Tilemap_Decor static (scene)
        //   0       : Runtime Ground (có collision - platformer ground) - VISIBLE
        //   1       : Runtime Platform (có collision - platformer platforms) - VISIBLE
        //   2       : Runtime Asteroid (có collision) - VISIBLE
        //   3       : Runtime Hazard (trigger) - VISIBLE
        //   4+      : Player, Enemies, Bullets (giữ nguyên)
        _layers[0] = CreateLayer("DeepSpace", -6, speedDeepSpace, PaintDeepSpace);
        _layers[1] = CreateLayer("Station",   -4, speedStation,   PaintStation);
        _layers[2] = CreateLayer("Decor",     -3, speedDecor,     PaintDecor);
        _layers[3] = CreateLayer("Ground",     0, speedGround,    PaintGround,   solidCollider: true);
        _layers[4] = CreateLayer("Platform",   1, speedPlatform,  PaintPlatform, solidCollider: true);
        _layers[5] = CreateLayer("Asteroid",   2, speedAsteroid,  PaintAsteroid, solidCollider: true);
        _layers[6] = CreateLayer("Hazard",     3, speedHazard,    PaintHazard,   triggerCollider: true);
        
        Debug.Log("[LevelTilemapSpawner] All 7 tilemap layers created with proper sorting order for visibility");
    }

    private LayerData CreateLayer(
        string name, int sortOrder, float speed,
        System.Action<Tilemap> paintFunc,
        bool solidCollider = false,
        bool triggerCollider = false)
    {
        // Mỗi layer có Grid riêng để di chuyển độc lập
        GameObject gridGO = new GameObject($"TilemapGrid_{name}", typeof(Grid));
        gridGO.transform.SetParent(transform);
        gridGO.transform.position = new Vector3(_camStartX, _camStartY, 0f);
        gridGO.GetComponent<Grid>().cellSize = Vector3.one;

        // Tilemap con
        GameObject tmGO = new GameObject($"Tilemap_{name}", typeof(Tilemap), typeof(TilemapRenderer));
        tmGO.transform.SetParent(gridGO.transform);
        tmGO.transform.localPosition = Vector3.zero;

        TilemapRenderer renderer = tmGO.GetComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder     = sortOrder;
        renderer.maskInteraction  = SpriteMaskInteraction.None;

        Tilemap tm = tmGO.GetComponent<Tilemap>();

        // Colliders
        if (solidCollider)
        {
            TilemapCollider2D col = tmGO.AddComponent<TilemapCollider2D>();
            col.isTrigger = false;
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D comp = tmGO.AddComponent<CompositeCollider2D>();
            Rigidbody2D rb = tmGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }
        else if (triggerCollider)
        {
            TilemapCollider2D col = tmGO.AddComponent<TilemapCollider2D>();
            col.isTrigger = true;
        }

        // Paint tiles
        paintFunc(tm);

        // Force compute bounds ngay
        tm.CompressBounds();
        float height = tm.localBounds.size.y;
        bool  ready  = height > 0f;
        
        // Verify tiles were actually painted
        if (tm.GetUsedTilesCount() == 0)
        {
            Debug.LogWarning($"[LevelTilemapSpawner] Layer {name} has no tiles painted!");
        }
        else
        {
            Debug.Log($"[LevelTilemapSpawner] Layer {name} created with {tm.GetUsedTilesCount()} tiles, sortingOrder={sortOrder}");
        }

        var layerData = new LayerData
        {
            gridRoot    = gridGO,
            tilemap     = tm,
            scrollSpeed = speed,
            tileHeight  = height,
            startPos    = gridGO.transform.position,
            initialized = ready
        };

        if (!ready)
            Debug.LogWarning($"[LevelTilemapSpawner] Layer {name}: bounds not ready yet, will retry.");

        return layerData;
    }

    private void SetupHazard()
    {
        if (_layers == null || _layers.Length < 7) return;
        Tilemap hazardTm = _layers[6].tilemap;
        if (hazardTm == null) return;

        TilemapHazardAdvanced hazard = hazardTm.gameObject.AddComponent<TilemapHazardAdvanced>();
        hazard.damagePerHit     = 1;
        hazard.damageInterval   = Mathf.Lerp(0.9f, 0.5f, (levelIndex - 1) / 2f);
        hazard.hazardFlashColor = new Color(1f, 0.15f, 0.05f, 0.9f);
        hazard.damageSFXKey     = "sfx_explosion_small";
    }

    private void SetupMovingPlatforms()
    {
        if (_layers == null || _layers.Length < 7) return;
        Tilemap platformTm = _layers[4].tilemap;
        if (platformTm == null) return;

        // Add moving platform behavior to some platforms
        TilemapInteractive interactive = platformTm.gameObject.AddComponent<TilemapInteractive>();
        interactive.interactionType = TilemapInteractive.TileInteractionType.MovingPlatform;
        interactive.moveAmplitudeX = 3f + levelIndex * 0.5f;
        interactive.moveAmplitudeY = 0f;
        interactive.moveFrequency = 0.3f + levelIndex * 0.1f;
    }

    // ─────────────────────────────────────────────────────────────────────
    // PAINT
    // ─────────────────────────────────────────────────────────────────────

    private void PaintDeepSpace(Tilemap tm)
    {
        Sprite[] sprites = tileSetData.deepSpaceSprites;
        if (sprites == null || sprites.Length == 0) return;
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                float n   = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                int   idx = Mathf.FloorToInt(n * sprites.Length) % sprites.Length;
                SetTile(tm, x, y, sprites[idx]);
            }
    }

    private void PaintStation(Tilemap tm)
    {
        Sprite[] sprites = tileSetData.stationSprites;
        if (sprites == null || sprites.Length == 0) return;
        for (int y = 0; y < mapHeight; y++)
        {
            // Create more varied station walls with patterns
            if (y % 3 != 2)
            {
                SetTile(tm, 0,            y, PickRandom(sprites));
                SetTile(tm, 1,            y, PickRandom(sprites));
                SetTile(tm, mapWidth - 1, y, PickRandom(sprites));
                SetTile(tm, mapWidth - 2, y, PickRandom(sprites));
            }
            // Add horizontal beams with more frequency
            if (y % 6 == 0)
                for (int x = 2; x <= mapWidth - 3; x++)
                    SetTile(tm, x, y, PickRandom(sprites));
            
            // Add vertical pillars
            if (y % 12 == 0 && y > 5)
            {
                SetTile(tm, mapWidth / 4, y, PickRandom(sprites));
                SetTile(tm, mapWidth / 2, y, PickRandom(sprites));
                SetTile(tm, mapWidth * 3 / 4, y, PickRandom(sprites));
            }
        }
    }

    private void PaintDecor(Tilemap tm)
    {
        Sprite[] sprites = tileSetData.decorSprites;
        if (sprites == null || sprites.Length == 0) return;
        float density = 0.12f + levelIndex * 0.04f;
        for (int y = 0; y < mapHeight; y++)
            for (int x = 2; x < mapWidth - 2; x++)
                if (Random.value < density)
                    SetTile(tm, x, y, PickRandom(sprites));
    }

    private void PaintGround(Tilemap tm)
    {
        Sprite[] sprites = tileSetData.groundSprites;
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[LevelTilemapSpawner] Ground sprites not available, using station sprites as fallback");
            sprites = tileSetData.stationSprites;
        }
        if (sprites == null || sprites.Length == 0) return;
        
        int groundTileCount = 0;
        // Create ground layers at bottom and middle sections with varied patterns
        for (int y = 0; y < mapHeight; y++)
        {
            // Ground at bottom every 8 tiles (creating platforms)
            if (y % 8 == 0 || y % 8 == 1)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // Leave gaps for player to navigate through
                    if (x % 12 < 8 || Random.value < 0.7f)
                    {
                        SetTile(tm, x, y, PickRandom(sprites));
                        groundTileCount++;
                    }
                }
            }
            // Random ground patches with better distribution
            else if (Random.value < 0.12f + levelIndex * 0.03f)
            {
                int startX = Random.Range(2, mapWidth - 6);
                int width = Random.Range(3, 8 + levelIndex);
                for (int x = startX; x < startX + width && x < mapWidth - 2; x++)
                {
                    SetTile(tm, x, y, PickRandom(sprites));
                    groundTileCount++;
                }
            }
            // Add stepping stones pattern
            if (y % 10 == 5 && levelIndex > 1)
            {
                for (int x = 4; x < mapWidth - 4; x += 4)
                {
                    SetTile(tm, x, y, PickRandom(sprites));
                    SetTile(tm, x + 1, y, PickRandom(sprites));
                    groundTileCount += 2;
                }
            }
        }
        Debug.Log($"[LevelTilemapSpawner] Ground layer painted: {groundTileCount} tiles");
    }

    private void PaintPlatform(Tilemap tm)
    {
        Sprite[] sprites = tileSetData.platformSprites;
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[LevelTilemapSpawner] Platform sprites not available, using decor sprites as fallback");
            sprites = tileSetData.decorSprites;
        }
        if (sprites == null || sprites.Length == 0) return;
        
        int platformTileCount = 0;
        // Create platforms at various heights with varied patterns
        int platformCount = 5 + levelIndex * 2;
        for (int i = 0; i < platformCount; i++)
        {
            int y = Random.Range(5, mapHeight - 5);
            int startX = Random.Range(3, mapWidth / 2);
            int width = Random.Range(4, 10 + levelIndex);
            
            // Create platform with varied width
            for (int x = startX; x < startX + width && x < mapWidth - 3; x++)
            {
                SetTile(tm, x, y, PickRandom(sprites));
                platformTileCount++;
            }
            
            // Add some floating platforms above
            if (levelIndex > 1 && Random.value < 0.4f)
            {
                int floatY = y + Random.Range(2, 4);
                int floatWidth = Random.Range(2, 5);
                int floatStartX = startX + Random.Range(0, width - floatWidth);
                for (int x = floatStartX; x < floatStartX + floatWidth && x < mapWidth - 3; x++)
                {
                    SetTile(tm, x, floatY, PickRandom(sprites));
                    platformTileCount++;
                }
            }
        }
        
        // Add diagonal platform patterns for higher levels
        if (levelIndex >= 2)
        {
            for (int y = 10; y < mapHeight - 10; y += 6)
            {
                int diagonalX = (y / 6) % 2 == 0 ? 3 : mapWidth - 8;
                for (int x = diagonalX; x < diagonalX + 5 && x < mapWidth - 3; x++)
                {
                    SetTile(tm, x, y, PickRandom(sprites));
                    platformTileCount++;
                }
            }
        }
        Debug.Log($"[LevelTilemapSpawner] Platform layer painted: {platformTileCount} tiles");
    }

    private void PaintAsteroid(Tilemap tm)
    {
        Sprite[] sprites = tileSetData.asteroidSprites;
        if (sprites == null || sprites.Length == 0) return;
        for (int zone = 0; zone < mapHeight / 5; zone++)
        {
            int baseY = zone * 5 + 1;
            int gapX  = Random.Range(3, mapWidth - 5);
            int gapW  = Mathf.Max(3, 5 - levelIndex);
            for (int x = 0; x < mapWidth; x++)
            {
                if (x >= gapX && x < gapX + gapW) continue;
                float edge = Mathf.Abs((float)x / mapWidth - 0.5f) * 2f;
                if (Random.value < 0.3f + edge * 0.4f)
                    SetTile(tm, x, baseY, PickRandom(sprites));
            }
            
            // Add asteroid clusters for higher levels
            if (levelIndex > 1 && Random.value < 0.3f)
            {
                int clusterX = Random.Range(2, mapWidth - 4);
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int cx = clusterX + dx;
                        int cy = baseY + dy;
                        if (cx >= 0 && cx < mapWidth && cy >= 0 && cy < mapHeight)
                            SetTile(tm, cx, cy, PickRandom(sprites));
                    }
                }
            }
        }
    }

    private void PaintHazard(Tilemap tm)
    {
        Sprite[] sprites = tileSetData.hazardSprites;
        if (sprites == null || sprites.Length == 0) return;
        int totalZones  = mapHeight / 10;
        int hazardZones = Mathf.Min(levelIndex * 2, totalZones);
        for (int i = 0; i < hazardZones; i++)
        {
            int baseY  = Random.Range(0, totalZones) * 10 + 3;
            int startX = Random.Range(3, mapWidth / 2);
            int w      = Random.Range(2, 4 + levelIndex);
            for (int x = startX; x < Mathf.Min(startX + w, mapWidth - 3); x++)
            {
                SetTile(tm, x, baseY,     PickRandom(sprites));
                SetTile(tm, x, baseY + 1, PickRandom(sprites));
            }
            
            // Add vertical hazard strips for higher levels
            if (levelIndex >= 2 && Random.value < 0.4f)
            {
                int vertX = Random.Range(5, mapWidth - 5);
                for (int y = baseY; y < baseY + 4 && y < mapHeight; y++)
                {
                    SetTile(tm, vertX, y, PickRandom(sprites));
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    private void SetTile(Tilemap tm, int x, int y, Sprite sprite)
    {
        if (tm == null || sprite == null) return;
        if (!_tileCache.TryGetValue(sprite, out Tile tile))
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name         = sprite.name;
            tile.sprite       = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.color        = Color.white;
            _tileCache[sprite] = tile;
            _tilePool.Add(tile);         // strong reference → không bị GC
        }
        tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private Sprite PickRandom(Sprite[] arr) =>
        arr != null && arr.Length > 0 ? arr[Random.Range(0, arr.Length)] : null;

    // ─────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────

    // Base speeds (lưu để SetDifficultyScale hoạt động đúng)
    private float[] _baseSpeeds;

    public void SetDifficultyScale(float scale)
    {
        if (_layers == null || _baseSpeeds == null) return;
        float clamped = Mathf.Clamp(scale, 0.1f, 10f);
        for (int i = 0; i < _layers.Length; i++)
            _layers[i].scrollSpeed = _baseSpeeds[i] * clamped;
    }

    private void OnDestroy()
    {
        foreach (Tile t in _tilePool)
            if (t != null) Destroy(t);
        _tilePool.Clear();
        _tileCache.Clear();
    }
}
