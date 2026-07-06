using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public enum WaveState
{
    Countdown,
    Spawning,
    Battling,
    LevelComplete
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Data")]
    [Tooltip("Kéo thả các file WaveData theo đúng thứ tự xuất hiện")]
    public WaveData[] waves;

    [Header("Timing")]
    public float timeBetweenWaves = 4f;

    [Header("Spawn Boundaries")]
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public float spawnPositionY = 6f;

    [Header("UI")]
    public TMP_Text waveStatusText;

    [Header("References")]
    public ObjectPool enemyPool;

    public UnityEvent<int> OnWaveChanged;
    public UnityEvent<int> OnEnemyKilled;

    private WaveState state = WaveState.Countdown;
    private int currentWaveIndex = -1;
    private float waveCountdown;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();

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
            // Start the standard gameplay music
            AudioManager.Instance?.PlayBGM("bgm_gameplay");

            waveCountdown = timeBetweenWaves;
            state = WaveState.Countdown;
            currentWaveIndex = 0;
            UpdateUI();
        }
    }

    private void Update()
    {
        // Clean up null/dead enemies from tracking list every frame
        activeEnemies.RemoveAll(e => e == null || !e.activeSelf);

        switch (state)
        {
            case WaveState.Countdown:
                HandleCountdown();
                break;
            case WaveState.Spawning:
                // Spawning is driven by coroutine, nothing to do in Update
                break;
            case WaveState.Battling:
                HandleBattling();
                break;
        }
    }

    #region State Handlers

    private void HandleCountdown()
    {
        if (currentWaveIndex >= waves.Length)
        {
            CompleteLevel();
            return;
        }

        waveCountdown -= Time.deltaTime;

        if (waveCountdown <= 0f)
        {
            StartWaveInternal(currentWaveIndex);
        }

        UpdateUI();
    }

    private void HandleBattling()
    {
        // Wait for player to clear all enemies
        if (activeEnemies.Count > 0)
        {
            UpdateUI();
            return;
        }

        // --- Special Logic for Boss in Level 3 ---
        // If this was the last wave (Boss) in Level 3, don't play the SFX here
        // as the BossController already handles bgm_winner.
        bool isBossLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level3");
        bool isLastWave = (currentWaveIndex == waves.Length - 1);

        if (!(isBossLevel && isLastWave))
        {
            AudioManager.Instance?.PlaySFX("sfx_wave_clear");
        }

        // All enemies cleared — advance to next wave
        state = WaveState.Countdown;
        waveCountdown = timeBetweenWaves;
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Length)
        {
            CompleteLevel();
        }
        else
        {
            UpdateUI();
        }
    }

    #endregion

    #region Wave Logic

    private void StartWaveInternal(int index)
    {
        if (index < 0 || index >= waves.Length) return;

        state = WaveState.Spawning;
        StartCoroutine(SpawnWaveRoutine(waves[index]));
    }

    private IEnumerator SpawnWaveRoutine(WaveData data)
    {
        if (waveStatusText != null)
        {
            waveStatusText.text = $"WAVE {currentWaveIndex + 1}";
        }

        OnWaveChanged?.Invoke(currentWaveIndex + 1);

        // Small delay so player can read announcement
        yield return new WaitForSeconds(1f);

        if (waveStatusText != null)
        {
            waveStatusText.text = "";
        }

        // Spawn each enemy in sequence
        for (int i = 0; i < data.enemyCount; i++)
        {
            // 1. Choose prefab
            GameObject prefabToSpawn = data.enemyPrefab;
            if (data.enemyPrefabs != null && data.enemyPrefabs.Length > 0)
            {
                prefabToSpawn = data.enemyPrefabs[i % data.enemyPrefabs.Length];
            }

            if (prefabToSpawn == null) continue;

            // 2. Calculate X position from percentage
            float pctX = 0.5f;
            if (data.spawnPositionsX != null && data.spawnPositionsX.Length > i)
            {
                pctX = data.spawnPositionsX[i];
                if (pctX > 1f) pctX /= 100f;
            }

            float worldX;
            if (leftSpawnPoint != null && rightSpawnPoint != null)
            {
                worldX = Mathf.Lerp(leftSpawnPoint.position.x, rightSpawnPoint.position.x, pctX);
            }
            else
            {
                // Fallback: use camera viewport (0 to 1)
                Camera cam = Camera.main;
                if (cam != null)
                {
                    float zDist = Mathf.Abs(cam.transform.position.z);
                    Vector3 leftEdge = cam.ViewportToWorldPoint(new Vector3(0.05f, 0.5f, zDist));
                    Vector3 rightEdge = cam.ViewportToWorldPoint(new Vector3(0.95f, 0.5f, zDist));
                    worldX = Mathf.Lerp(leftEdge.x, rightEdge.x, pctX);
                }
                else
                {
                    worldX = Mathf.Lerp(-8f, 8f, pctX);
                }
            }

            Vector3 spawnPos = new Vector3(worldX, spawnPositionY, 0f);

            // 3. Spawn
            GameObject enemy = SpawnEnemy(prefabToSpawn, spawnPos, data.speedMultiplier);

            // 4. Delay between spawns
            if (i < data.enemyCount - 1)
            {
                yield return new WaitForSeconds(data.spawnDelay);
            }
        }

        // All spawned — switch to battle state
        state = WaveState.Battling;
        UpdateUI();
    }

    public GameObject SpawnEnemy(GameObject prefab, Vector3 position, float speedMult = 1f)
    {
        GameObject enemy = null;
        Debug.Log($"[WaveManager] SpawnEnemy requested for prefab: {prefab?.name} at position: {position}");

        // Find the correct pool for this prefab
        ObjectPool pool = enemyPool;
        if (pool == null || pool.prefab != prefab)
        {
            ObjectPool matchedPool = null;
            ObjectPool[] allPools = FindObjectsByType<ObjectPool>(FindObjectsInactive.Exclude);
            foreach (var p in allPools)
            {
                if (p.prefab == prefab)
                {
                    matchedPool = p;
                    break;
                }
            }
            pool = matchedPool;
        }

        // Dynamically create pool if not present in the scene, ensuring optimization
        if (pool == null && prefab != null)
        {
            GameObject container = GameObject.Find("ObjectPoolContainer");
            Transform parentTransform = container != null ? container.transform : null;

            GameObject newPoolGo = new GameObject($"{prefab.name}Pool_Dynamic");
            if (parentTransform != null)
            {
                newPoolGo.transform.SetParent(parentTransform);
            }

            pool = newPoolGo.AddComponent<ObjectPool>();
            pool.Initialize(prefab, 20, 5);
            Debug.Log($"[WaveManager] Dynamically created ObjectPool for '{prefab.name}' to optimize memory and GC allocation.");
        }

        if (pool != null)
        {
            Debug.Log($"[WaveManager] Found ObjectPool '{pool.gameObject.name}' for prefab '{prefab.name}'");
            enemy = pool.Get(position, Quaternion.identity);
        }
        else
        {
            Debug.Log($"[WaveManager] No ObjectPool found for prefab '{prefab.name}', calling Instantiate");
            enemy = Instantiate(prefab, position, Quaternion.identity);
        }

        // Apply speed multiplier
        if (enemy != null)
        {
            Debug.Log($"[WaveManager] Successfully spawned enemy '{enemy.name}', activeSelf={enemy.activeSelf}, position={enemy.transform.position}");
            // Register enemy in active list if not already there
            if (!activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
                UpdateUI();
            }

            EnemyDrone drone = enemy.GetComponent<EnemyDrone>();
            if (drone != null)
            {
                drone.moveSpeed = 2f * speedMult;
            }

            EnemyHunter hunter = enemy.GetComponent<EnemyHunter>();
            if (hunter != null)
            {
                hunter.moveSpeed = 3.5f * speedMult;
                hunter.verticalSpeed = 1.0f * speedMult;
            }

            ObstacleMine mine = enemy.GetComponent<ObstacleMine>();
            if (mine != null)
            {
                mine.moveSpeed = 1.5f * speedMult;
            }
        }
        else
        {
            Debug.LogError($"[WaveManager] Failed to spawn enemy for prefab '{prefab?.name}'");
        }

        return enemy;
    }

    #endregion

    #region Enemy Tracking

    /// <summary>Called by EnemyHealth.Die() when an enemy is killed by the player.</summary>
    public void OnEnemyDestroyed(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
        OnEnemyKilled?.Invoke(activeEnemies.Count);
    }

    #endregion

    #region Level Flow

    private void CompleteLevel()
    {
        state = WaveState.LevelComplete;
        if (waveStatusText != null)
        {
            waveStatusText.text = "LEVEL COMPLETED!";
        }

        bool isLevel3 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level3");
        if (isLevel3)
        {
            AudioManager.Instance?.StopAllLevelSounds();
            AudioManager.Instance?.PlayBGM("bgm_winner", true);
        }

        LevelManager.Instance?.LevelComplete();
    }

    #endregion

    #region UI

    private void UpdateUI()
    {
        if (waveStatusText == null) return;

        switch (state)
        {
            case WaveState.Countdown:
                int displayWave = currentWaveIndex + 1;
                if (displayWave > waves.Length) displayWave = waves.Length;
                waveStatusText.text = $"WAVES: {displayWave}";
                break;

            case WaveState.Battling:
                waveStatusText.text = $"ENEMIES: {activeEnemies.Count}";
                break;

            case WaveState.Spawning:
                waveStatusText.text = $"WAVES: {currentWaveIndex + 1}";
                break;

            case WaveState.LevelComplete:
                waveStatusText.text = "LEVEL COMPLETED!";
                break;
        }
    }

    #endregion

    #region Public API

    public WaveState GetState() => state;
    public int GetCurrentWave() => currentWaveIndex + 1;
    public int GetActiveEnemyCount() => activeEnemies.Count;
    public bool IsWaveInProgress() => state == WaveState.Battling || state == WaveState.Spawning;

    #endregion

#if UNITY_EDITOR
    private float autoPlayTimer = 0f;
    private bool pressedB = false;
    private bool quitTriggered = false;

    private void AutoPlayDebug()
    {
        if (System.Environment.CommandLine.Contains("-autoplay"))
        {
            autoPlayTimer += Time.unscaledDeltaTime;
            if (autoPlayTimer > 3f && !pressedB)
            {
                pressedB = true;
                Debug.Log("[Autoplay] 3 seconds elapsed, pressing B to spawn boss...");
                StopAllCoroutines();
                foreach (var enemy in activeEnemies.ToArray())
                {
                    if (enemy != null) enemy.SetActive(false);
                }
                activeEnemies.Clear();
                currentWaveIndex = waves.Length - 1;
                state = WaveState.Countdown;
                waveCountdown = 0.5f;
                UpdateUI();
            }

            if (autoPlayTimer > 8.5f && !bossKilled)
            {
                bossKilled = true;
                Debug.Log("[Autoplay] 8.5 seconds elapsed, killing Boss for music / death sequence test...");
                foreach (var enemy in activeEnemies.ToArray())
                {
                    if (enemy != null)
                    {
                        EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
                        if (eh != null) eh.TakeDamage(9999);
                    }
                }
            }

            if (autoPlayTimer > 15f && !quitTriggered)
            {
                quitTriggered = true;
                Debug.Log("[Autoplay] 15 seconds elapsed, exiting play mode and quitting Unity...");
                UnityEditor.EditorApplication.isPlaying = false;
                UnityEditor.EditorApplication.Exit(0);
            }
        }
    }
    private bool bossKilled = false;
#endif
}
