using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [System.Serializable]
    public class WaveData
    {
        public GameObject enemyPrefab;
        public int enemyCount = 4;
        public float[] spawnPositionsX; // screen-width percentages (0–1)
        public float speedMultiplier = 1f;
        public float spawnDelay = 0.3f;
    }

    [Header("Level Waves")]
    public WaveData[] waves;

    [Header("References")]
    public ObjectPool enemyPool;

    public UnityEvent<int> OnWaveChanged;
    public UnityEvent<int> OnEnemyKilled;

    private int currentWaveIndex = -1;
    private int activeEnemyCount = 0;
    private bool waveInProgress = false;

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
            float pctX = data.spawnPositionsX.Length > i
                ? data.spawnPositionsX[i]
                : Random.Range(0.1f, 0.9f);

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
        activeEnemyCount++;

        GameObject enemy;
        if (enemyPool != null)
        {
            enemy = enemyPool.Get(position, Quaternion.identity);
        }
        else
        {
            enemy = Instantiate(prefab, position, Quaternion.identity);
        }

        // Apply speed multiplier if the enemy supports it
        // Note: Enemy death tracking should be hooked via ObjectPool events or a dedicated interface

        return enemy;
    }

    public void RegisterEnemy()
    {
        activeEnemyCount++;
    }

    private void OnEnemyDestroyed()
    {
        activeEnemyCount--;
        OnEnemyKilled?.Invoke(activeEnemyCount);
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
