using UnityEngine;

/// <summary>
/// ScriptableObject chứa tham chiếu tới TẤT CẢ sprite từ 5 tileset.
/// Được điền tự động bởi TilemapSpawnerSetupTool (Editor menu).
/// Được LevelTilemapSpawner đọc trong cả Editor lẫn Build.
///
/// Tạo asset: Assets/Resources/TileSetData.asset (dùng Editor tool).
/// </summary>
[CreateAssetMenu(fileName = "TileSetData", menuName = "Galaxy Defender/Tile Set Data")]
public class TileSetData : ScriptableObject
{
    [Header("tiles_deepspace.png — 9 tiles (3×3, 166px)")]
    public Sprite[] deepSpaceSprites;

    [Header("tiles_station.png — 16 tiles (4×4, 256px)")]
    public Sprite[] stationSprites;

    [Header("tiles_decor.png — mixed")]
    public Sprite[] decorSprites;

    [Header("tiles_asteroid.png — 14 tiles (4×4, 125px)")]
    public Sprite[] asteroidSprites;

    [Header("tiles_hazard.png — mixed")]
    public Sprite[] hazardSprites;

    [Header("Ground tiles (for platformer gameplay)")]
    public Sprite[] groundSprites;

    [Header("Platform tiles (for platformer gameplay)")]
    public Sprite[] platformSprites;

    /// <summary>Kiểm tra tất cả arrays đã được điền chưa.</summary>
    public bool IsValid()
    {
        return deepSpaceSprites != null && deepSpaceSprites.Length > 0
            && stationSprites   != null && stationSprites.Length   > 0
            && asteroidSprites  != null && asteroidSprites.Length  > 0;
    }

    public int TotalTileCount =>
        (deepSpaceSprites?.Length ?? 0) +
        (stationSprites?.Length   ?? 0) +
        (decorSprites?.Length     ?? 0) +
        (asteroidSprites?.Length  ?? 0) +
        (hazardSprites?.Length    ?? 0) +
        (groundSprites?.Length    ?? 0) +
        (platformSprites?.Length  ?? 0);
}
