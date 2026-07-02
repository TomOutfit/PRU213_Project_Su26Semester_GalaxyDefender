using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RuntimeSpriteFixer
{
    public static void EnsureSprite(SpriteRenderer sr, string spritePath, bool force = false)
    {
        if (sr == null) return;
        if (sr.sprite != null && !force) return;

#if UNITY_EDITOR
        // 1. Try native AssetDatabase load first
        Sprite loadedSprite = null;
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        foreach (var asset in subAssets)
        {
            if (asset is Sprite spr)
            {
                loadedSprite = spr;
                break;
            }
        }

        if (loadedSprite == null)
        {
            // Try to force configure and import
            TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }
                AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport);
            }
            
            subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            foreach (var asset in subAssets)
            {
                if (asset is Sprite spr)
                {
                    loadedSprite = spr;
                    break;
                }
            }
        }

        // 2. Fallback: Load raw file bytes and create sprite on the fly (guaranteed to work in Play Mode)
        if (loadedSprite == null)
        {
            if (System.IO.File.Exists(spritePath))
            {
                try
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(spritePath);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(fileData))
                    {
                        tex.filterMode = FilterMode.Point; // Preserves pixel art crispness
                        
                        Rect spriteRect = new Rect(0, 0, tex.width, tex.height);
                        if (spritePath.Contains("_sheet"))
                        {
                            if (spritePath.Contains("player_thruster_sheet"))
                            {
                                spriteRect = new Rect(0, tex.height - (tex.height / 4), tex.width, tex.height / 4);
                            }
                            else if (spritePath.Contains("enemy_drone_sheet"))
                            {
                                spriteRect = new Rect(0, tex.height - (tex.height / 2), tex.width / 2, tex.height / 2);
                            }
                            else if (spritePath.Contains("enemy_hunter_sheet"))
                            {
                                spriteRect = new Rect(0, 0, tex.width / 4, tex.height);
                            }
                            else if (spritePath.Contains("obstacle_mine_sheet"))
                            {
                                spriteRect = new Rect(0, tex.height - (tex.height / 2), tex.width / 3, tex.height / 2);
                            }
                            else if (spritePath.Contains("explosion_small_sheet") || spritePath.Contains("explosion_large_sheet"))
                            {
                                spriteRect = new Rect(0, tex.height - (tex.height / 4), tex.width / 4, tex.height / 4);
                            }
                            else if (spritePath.Contains("effect_hit_sheet"))
                            {
                                spriteRect = new Rect(0, 0, tex.width / 5, tex.height);
                            }
                        }
                        
                        loadedSprite = Sprite.Create(tex, spriteRect, new Vector2(0.5f, 0.5f), GetPPUForPath(spritePath));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[RuntimeSpriteFixer] Fallback failed for '{spritePath}': {ex.Message}");
                }
            }
        }

        if (loadedSprite != null)
        {
            sr.sprite = loadedSprite;
        }
        else
        {
            Debug.LogError($"[RuntimeSpriteFixer] Failed to load sprite at '{spritePath}' even with fallback.");
        }
#endif
    }

    private static float GetPPUForPath(string path)
    {
        string p = path.Replace('\\', '/');
        if (p.Contains("Player/"))
        {
            if (p.Contains("player_ship.png") || p.Contains("player_thruster_sheet.png"))
                return 560f;
            return 850f;
        }
        if (p.Contains("Enemies/"))
        {
            if (p.Contains("enemy_boss"))
                return 200f;
            if (p.Contains("enemy_drone") || p.Contains("enemy_hunter"))
                return 250f;
            return 500f;
        }
        if (p.Contains("Bullets/"))
        {
            if (p.Contains("bullet_boss"))
                return 800f;
            if (p.Contains("bullet_enemy") || p.Contains("bullet_player"))
                return 800f;
            return 1600f;
        }
        if (p.Contains("PowerUps/"))
        {
            if (p.Contains("powerup_health") || p.Contains("powerup_score") || p.Contains("powerup_shield"))
                return 500f;
            if (p.Contains("glow_ring"))
                return 64f;
            return 1000f;
        }
        return 100f;
    }
}
