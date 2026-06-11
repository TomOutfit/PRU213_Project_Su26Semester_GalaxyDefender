using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Level Waves")]
    public WaveData[] waves;

    [Header("References")]
    public ObjectPool enemyPool;

    public UnityEvent<int> OnWaveChanged;
    public UnityEvent<int> OnEnemyKilled;

    private int currentWaveIndex = -1;
    private int activeEnemyCount = 0;
    private bool waveInProgress = false;
    private List<GameObject> activeEnemies = new List<GameObject>();

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
        if (waves != null && waves.Length > 0)
        {
            StartWave(0);
        }
    }

    public void StartWave(int index)
    {
        if (index < 0 || index >= waves.Length) return;

        currentWaveIndex = index;
        waveInProgress = true;
        activeEnemies.Clear();
        OnWaveChanged?.Invoke(currentWaveIndex + 1);
        StartCoroutine(SpawnWave(waves[index]));
    }

    private IEnumerator SpawnWave(WaveData data)
    {
        Camera cam = Camera.main;
        float topY = cam.ViewportToWorldPoint(Vector3.up).y;

        for (int i = 0; i < data.enemyCount; i++)
        {
            float pctX = (data.spawnPositionsX != null && data.spawnPositionsX.Length > i)
                ? data.spawnPositionsX[i]
                : Random.Range(0.1f, 0.9f);

            // Handle percentages defined as 0-100 or 0-1
            if (pctX > 1f) pctX /= 100f;

            float worldX = Mathf.Lerp(
                cam.ViewportToWorldPoint(Vector3.left).x,
                cam.ViewportToWorldPoint(Vector3.right).x,
                pctX
            );

            Vector3 spawnPos = new Vector3(worldX, topY + 1f, 0f);

            GameObject prefabToSpawn = data.enemyPrefab;
            if (data.enemyPrefabs != null && data.enemyPrefabs.Length > 0)
            {
                prefabToSpawn = data.enemyPrefabs[i % data.enemyPrefabs.Length];
            }

            SpawnEnemy(prefabToSpawn, spawnPos, data.speedMultiplier);

            if (i < data.enemyCount - 1)
                yield return new WaitForSeconds(data.spawnDelay);
        }

        StartCoroutine(PollWaveCleared());
    }

    public GameObject SpawnEnemy(GameObject prefab, Vector3 position, float speedMult = 1f)
    {
        activeEnemyCount++;

        GameObject enemy = null;
        ObjectPool pool = enemyPool;

        // Try to find pool managing this prefab in the scene if not explicitly assigned
        if (pool == null)
        {
            ObjectPool[] allPools = FindObjectsByType<ObjectPool>(FindObjectsInactive.Include);
            foreach (var p in allPools)
            {
                if (p.prefab == prefab)
                {
                    pool = p;
                    break;
                }
            }
        }

        if (pool != null)
        {
            enemy = pool.Get(position, Quaternion.identity);
        }
        else
        {
            enemy = Instantiate(prefab, position, Quaternion.identity);
        }

        // Apply speed multiplier if the enemy supports it
        EnemyDrone drone = enemy.GetComponent<EnemyDrone>();
        if (drone != null)
        {
            drone.moveSpeed = 2f * speedMult; // base speed is 2f
        }

        EnemyHunter hunter = enemy.GetComponent<EnemyHunter>();
        if (hunter != null)
        {
            hunter.moveSpeed = 3.5f * speedMult; // base tracking speed is 3.5f
            hunter.verticalSpeed = 1.0f * speedMult; // base vertical speed is 1.0f
        }

        activeEnemies.Add(enemy);
        return enemy;
    }

    public void RegisterEnemy()
    {
        activeEnemyCount++;
    }

    public void OnEnemyDestroyed(GameObject enemy = null)
    {
        activeEnemyCount--;
        OnEnemyKilled?.Invoke(activeEnemyCount);
    }

    private IEnumerator PollWaveCleared()
    {
        while (true)
        {
            bool anyActive = false;
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null && activeEnemies[i].activeSelf)
                {
                    anyActive = true;
                    break;
                }
            }

            if (!anyActive)
                break;

            yield return new WaitForSeconds(0.5f);
        }

        waveInProgress = false;
        yield return new WaitForSeconds(2f);

        if (currentWaveIndex + 1 < waves.Length)
        {
            StartWave(currentWaveIndex + 1);
        }
        else
        {
            LevelManager.Instance?.LevelComplete();
        }
    }

    public int GetCurrentWave() => currentWaveIndex + 1;
    public int GetActiveEnemyCount() => activeEnemyCount;
    public bool IsWaveInProgress() => waveInProgress;
}

