using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Power-Up Prefabs")]
    public GameObject[] powerUpPrefabs;

    [Header("Random Spawning Configuration")]
    public bool enableRandomSpawning = true;
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (enableRandomSpawning)
        {
            StartCoroutine(RandomSpawnRoutine());
        }
    }

    private System.Collections.IEnumerator RandomSpawnRoutine()
    {
        // Initial delay before first spawn
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (true)
        {
            float delay = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(delay);

            // Skip spawning if game is paused or level complete
            if (Mathf.Approximately(Time.timeScale, 0f)) continue;
            if (WaveManager.Instance != null && WaveManager.Instance.GetState() == WaveState.LevelComplete) continue;

            if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) continue;

            float worldX = 0f;
            float worldY = 0f;

            Camera cam = Camera.main;
            if (cam != null)
            {
                float zDist = Mathf.Abs(cam.transform.position.z);
                // Keep X within 10% to 90% of screen width to ensure it is visible and not clipped
                float pctX = Random.Range(0.1f, 0.9f);
                // Keep Y within 30% to 80% of screen height so it spawns on screen and drifts down
                float pctY = Random.Range(0.3f, 0.8f);

                Vector3 spawnWorldPos = cam.ViewportToWorldPoint(new Vector3(pctX, pctY, zDist));
                worldX = spawnWorldPos.x;
                worldY = spawnWorldPos.y;
            }
            else
            {
                // Fallback to static boundaries if no camera is found
                worldX = Random.Range(-6f, 6f);
                worldY = Random.Range(-2f, 4f);
            }

            Vector3 spawnPos = new Vector3(worldX, worldY, 0f);
            GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
            Instantiate(prefab, spawnPos, Quaternion.identity);

            Debug.Log($"[PowerUpManager] Spawned random {prefab.name} at {spawnPos}");
        }
    }

    public void Drop(Vector3 position)
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;
        
        // 100% Drop rate for maximum fun
        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
        GameObject powerUp = Instantiate(prefab, position, Quaternion.identity);
        
        Debug.Log($"[PowerUpManager] Dropped {prefab.name} at {position}");
    }
}

