using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SpriteDatabase", menuName = "Systems/SpriteDatabase")]
public class SpriteDatabase : ScriptableObject
{
    private static SpriteDatabase _instance;
    public static SpriteDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SpriteDatabase>("SpriteDatabase");
                if (_instance != null)
                {
                    _instance.InitializeCache();
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public struct SpriteEntry
    {
        public string pathOrName;
        public Sprite sprite;
    }

    public List<SpriteEntry> entries = new List<SpriteEntry>();
    private Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public void InitializeCache()
    {
        cache.Clear();
        foreach (var entry in entries)
        {
            if (entry.sprite == null) continue;
            
            // Normalize path (slash type and lowercase)
            string normPath = NormalizePath(entry.pathOrName);
            if (!cache.ContainsKey(normPath))
            {
                cache.Add(normPath, entry.sprite);
            }

            // Also cache by filename without extension for convenient lookup
            string filename = System.IO.Path.GetFileNameWithoutExtension(normPath);
            if (!string.IsNullOrEmpty(filename) && !cache.ContainsKey(filename))
            {
                cache.Add(filename, entry.sprite);
            }
        }
    }

    public Sprite GetSprite(string pathOrName)
    {
        if (string.IsNullOrEmpty(pathOrName)) return null;

        // Ensure cache is loaded
        if (cache.Count == 0 && entries.Count > 0)
        {
            InitializeCache();
        }

        string norm = NormalizePath(pathOrName);
        if (cache.TryGetValue(norm, out Sprite s)) return s;

        string filename = System.IO.Path.GetFileNameWithoutExtension(norm);
        if (cache.TryGetValue(filename, out s)) return s;

        return null;
    }

    private string NormalizePath(string path)
    {
        return path.Replace('\\', '/').ToLower();
    }
}
