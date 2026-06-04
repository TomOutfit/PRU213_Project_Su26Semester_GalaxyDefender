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
    private readonly HashSet<GameObject> trackedEnemies = new HashSet<GameObject>();

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
        OnWaveChanged?.Invoke(currentWaveIndex + 1);
        StartCoroutine(SpawnWave(waves[index]));
    }

    private IEnumerator SpawnWave(WaveData data)
    {
        Camera cam = Camera.main;
        float topY = cam.ViewportToWorldPoint(Vector3.up).y;

        for (int i = 0; i < data.enemyCount; i++)
        {
            float pctX = data.spawnPositionsX != null && data.spawnPositionsX.Length > i
                ? data.spawnPositionsX[i]
                : Random.Range(0.1f, 0.9f);

            if (pctX > 1f) pctX /= 100f;

            float worldX = Mathf.Lerp(
                cam.ViewportToWorldPoint(Vector3.left).x,
                cam.ViewportToWorldPoint(Vector3.right).x,
                pctX
            );

            Vector3 spawnPos = new Vector3(worldX, topY + 1f, 0f);
            SpawnEnemy(data.enemyPrefab, spawnPos, data.speedMultiplier);

            if (i < data.enemyCount - 1)
                yield return new WaitForSeconds(data.spawnDelay);
        }

        StartCoroutine(PollWaveCleared());
    }

    public GameObject SpawnEnemy(GameObject prefab, Vector3 position, float speedMult = 1f)
    {
        GameObject enemy;
        if (enemyPool != null)
        {
            enemy = enemyPool.Get(position, Quaternion.identity);
        }
        else
        {
            enemy = Instantiate(prefab, position, Quaternion.identity);
        }

        trackedEnemies.Add(enemy);
        activeEnemyCount++;

        StartCoroutine(TrackEnemy(enemy));
        return enemy;
    }

    private IEnumerator TrackEnemy(GameObject enemy)
    {
        while (enemy != null && enemy.activeSelf && trackedEnemies.Contains(enemy))
        {
            yield return null;
        }

        if (enemy != null && trackedEnemies.Contains(enemy))
        {
            trackedEnemies.Remove(enemy);
            activeEnemyCount--;
            OnEnemyKilled?.Invoke(activeEnemyCount);
        }
    }

    public void ReleaseEnemy(GameObject enemy)
    {
        if (trackedEnemies.Contains(enemy))
        {
            trackedEnemies.Remove(enemy);
            activeEnemyCount--;
            OnEnemyKilled?.Invoke(activeEnemyCount);
        }

        if (enemyPool != null)
        {
            enemyPool.Release(enemy);
        }
        else
        {
            Destroy(enemy);
        }
    }

    private IEnumerator PollWaveCleared()
    {
        while (activeEnemyCount > 0)
        {
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
