using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Three-phase boss. After a warning/entry sequence it fights in phases keyed to HP: Phase 1
/// (stationary, single aimed shot), Phase 2 (sine-wave strafe, faster fire) at ≤66% HP, and
/// Phase 3 (wider strafe, 3-bullet ±15° spread, spawns two drones once) at ≤33% HP. Swaps its
/// sprite per phase and runs a multi-blast death sequence that also applies an explosion force.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class BossController : MonoBehaviour
{
    public static UnityEvent OnBossDeadGlobal = new UnityEvent();

    [Header("Stats")]
    public int maxHP = 100000;
    public float spawnDuration = 3f;
    public float phase2MoveSpeed = 1.5f;
    public float phase3MoveSpeed = 2.0f;

    [Header("Prefabs & Points")]
    public GameObject bossBulletPrefab;
    public GameObject dronePrefab;
    public Transform bulletSpawnPoint;

    [Header("Bullet Damage (overrides bullet prefab defaults)")]
    [Tooltip("Override damage dealt per phase. Leave at 0 to use bullet prefab values.")]
    public int phase1Damage = 0;
    public int phase2Damage = 0;
    public int phase3Damage = 0;

    [Header("Bullet Speed (overrides bullet prefab defaults)")]
    [Tooltip("Override bullet speed per phase. Leave at 0 to use bullet prefab values.")]
    public float phase1Speed = 0f;
    public float phase2Speed = 0f;
    public float phase3Speed = 0f;

    [Header("Phase Visuals")]
    [Tooltip("Sprite shown during Phase 1 (full HP).")]
    public string phase1SpritePath = "Assets/Sprites/Enemies/enemy_boss.png";
    [Tooltip("Sprite shown during Phase 2 (66% HP).")]
    public string phase2SpritePath = "Assets/Sprites/Enemies/enemy_boss_phase2.png";
    [Tooltip("Sprite shown during Phase 3 (33% HP).")]
    public string phase3SpritePath = "Assets/Sprites/Enemies/enemy_boss_phase3.png";

    [HideInInspector]
    public bool isSpawning = true;
    [HideInInspector]
    public bool isDying = false;

    private int currentPhase = 1;
    private float fireTimer = 0f;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private BulletBoss bossBullet;
    private ObjectPool bossBulletPool;

    private Vector2 startPosition;
    private Vector2 targetSpawnPosition;

    private bool spawnedDronesPhase3 = false;

    [Header("Events")]
    public UnityEvent<int> OnPhaseChanged;
    public UnityEvent OnBossDead;

    private void Awake()
    {
        Debug.Log($"[BossController] Awake called on GameObject: {gameObject.name}");
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        Debug.Log($"[BossController] OnEnable called on GameObject: {gameObject.name}");
        isSpawning = true;
        isDying = false;
        currentPhase = 1;
        spawnedDronesPhase3 = false;

        // Reset sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        RuntimeSpriteFixer.EnsureSprite(sr, phase1SpritePath);

        // Reset positions
        Camera cam = Camera.main;
        float zDist = cam != null ? Mathf.Abs(cam.transform.position.z) : 10f;
        float topY = cam != null ? cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDist)).y : 5f;
        startPosition = new Vector2(0f, topY + 3f);
        targetSpawnPosition = new Vector2(0f, topY - 2.5f);
        transform.position = new Vector3(startPosition.x, startPosition.y, 0f);

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = startPosition;
            rb.simulated = true;
        }

        Debug.Log($"[BossController] topY={topY}, startPosition={startPosition}, targetSpawnPosition={targetSpawnPosition}, initialPosition={transform.position}");

        StartCoroutine(SpawnSequence());
    }

    private void OnDisable()
    {
        Debug.Log($"[BossController] OnDisable called on GameObject: {gameObject.name}");
    }

    private void OnDestroy()
    {
        Debug.Log($"[BossController] OnDestroy called on GameObject: {gameObject.name}");
    }

    private void Start()
    {
        Debug.Log($"[BossController] Start called on GameObject: {gameObject.name}");

        if (health != null)
        {
            health.maxHP = maxHP;
            health.points = 50000;
            health.OnHealthChanged.AddListener(OnHealthChanged);
            health.OnDeath.AddListener(Die);
        }

        GameObject poolObj = GameObject.Find("BulletBossPool");
        if (poolObj != null)
        {
            bossBulletPool = poolObj.GetComponent<ObjectPool>();
            // Cache the BulletBoss component reference from the prefab for direction usage
            if (bossBulletPrefab != null)
            {
                bossBullet = bossBulletPrefab.GetComponent<BulletBoss>();
            }
        }
    }

    private IEnumerator SpawnSequence()
    {
        Debug.Log($"[BossController] SpawnSequence started. isSpawning={isSpawning}");
        isSpawning = true;

        // Immediately turn off current music and play bgm_boss
        AudioManager.Instance?.StopBGM();
        AudioManager.Instance?.PlayBGM("bgm_boss", true);

        BossHUDController bossHUD = Object.FindAnyObjectByType<BossHUDController>();
        if (bossHUD != null)
        {
            bossHUD.ShowWarning(spawnDuration);
        }

        AudioManager.Instance?.PlaySFX("sfx_boss_warning");

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / spawnDuration;
            rb.position = Vector2.Lerp(startPosition, targetSpawnPosition, pct);
            if (elapsed % 0.5f < Time.deltaTime)
            {
                Debug.Log($"[BossController] Spawning Lerp: pct={pct}, rb.position={rb.position}, transform.position={transform.position}");
            }
            yield return null;
        }
        rb.position = targetSpawnPosition;
        Debug.Log($"[BossController] Spawn sequence complete. rb.position={rb.position}, transform.position={transform.position}");

        isSpawning = false;
        OnPhaseChanged?.Invoke(currentPhase);
    }

    private void Update()
    {
        if (isSpawning || isDying) return;

        UpdateMovement();
        UpdateShooting();
    }

    private void UpdateMovement()
    {
        Vector2 pos = rb.position;
        if (currentPhase == 1)
        {
            rb.position = targetSpawnPosition;
        }
        else if (currentPhase == 2)
        {
            Camera cam = Camera.main;
            float screenW = cam != null ? cam.ViewportToWorldPoint(Vector3.right).x - cam.ViewportToWorldPoint(Vector3.zero).x : 10f;
            float amplitude = screenW * 0.3f;
            float xOffset = amplitude * Mathf.Sin(Time.time * (2f * Mathf.PI / 4f));

            Vector2 targetPos = new Vector2(targetSpawnPosition.x + xOffset, targetSpawnPosition.y);
            rb.position = Vector2.MoveTowards(pos, targetPos, phase2MoveSpeed * Time.deltaTime);
        }
        else if (currentPhase == 3)
        {
            Camera cam = Camera.main;
            float screenW = cam != null ? cam.ViewportToWorldPoint(Vector3.right).x - cam.ViewportToWorldPoint(Vector3.zero).x : 10f;
            float amplitude = screenW * 0.35f;
            float xOffset = amplitude * Mathf.Sin(Time.time * (2f * Mathf.PI / 3f));

            Vector2 targetPos = new Vector2(targetSpawnPosition.x + xOffset, targetSpawnPosition.y);
            rb.position = Vector2.MoveTowards(pos, targetPos, phase3MoveSpeed * Time.deltaTime);
        }
    }

    private void UpdateShooting()
    {
        fireTimer += Time.deltaTime;
        float interval = 1.0f;
        if (currentPhase == 2) interval = 0.7f;
        else if (currentPhase == 3) interval = 0.4f;

        if (fireTimer >= interval)
        {
            fireTimer = 0f;
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector3 spawnPos = bulletSpawnPoint != null ? bulletSpawnPoint.position : transform.position;

        Vector2 dirToPlayer = Vector2.down;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            dirToPlayer = (player.transform.position - spawnPos).normalized;
        }

        if (currentPhase == 1 || currentPhase == 2)
        {
            ShootSingle(spawnPos, dirToPlayer);
        }
        else if (currentPhase == 3)
        {
            ShootSingle(spawnPos, dirToPlayer);
            
            // Left bullet rotated by -15 degrees
            Vector2 dirLeft = Quaternion.Euler(0, 0, -15) * dirToPlayer;
            ShootSingle(spawnPos, dirLeft);

            // Right bullet rotated by 15 degrees
            Vector2 dirRight = Quaternion.Euler(0, 0, 15) * dirToPlayer;
            ShootSingle(spawnPos, dirRight);
        }
    }

    private void ShootSingle(Vector3 position, Vector2 direction)
    {
        if (bossBulletPool != null && bossBulletPool.prefab != null)
        {
            GameObject bullet = bossBulletPool.Get(position, Quaternion.identity);
            if (bullet != null)
            {
                BulletBoss bb = bullet.GetComponent<BulletBoss>();
                if (bb != null)
                {
                    bb.SetPhase(currentPhase);
                    ApplyBulletOverrides(bb);
                    bb.SetDirection(direction);
                }
            }
        }
        else if (bossBulletPrefab != null)
        {
            GameObject bullet = Instantiate(bossBulletPrefab, position, Quaternion.identity);
            BulletBoss bb = bullet.GetComponent<BulletBoss>();
            if (bb != null)
            {
                bb.SetPhase(currentPhase);
                ApplyBulletOverrides(bb);
                bb.SetDirection(direction);
            }
        }

        AudioManager.Instance?.PlaySFX("sfx_shoot_boss");
    }

    /// <summary>Apply per-phase damage/speed overrides from the BossController inspector.</summary>
    private void ApplyBulletOverrides(BulletBoss bb)
    {
        if (bb == null) return;
        int dmg = currentPhase switch
        {
            2 => phase2Damage > 0 ? phase2Damage : bb.phase2Damage,
            3 => phase3Damage > 0 ? phase3Damage : bb.phase3Damage,
            _  => phase1Damage > 0 ? phase1Damage : bb.phase1Damage
        };
        float spd = currentPhase switch
        {
            2 => phase2Speed > 0f ? phase2Speed : bb.phase2Speed,
            3 => phase3Speed > 0f ? phase3Speed : bb.phase3Speed,
            _  => phase1Speed > 0f ? phase1Speed : bb.phase1Speed
        };
        bb.SetPhaseDamageAndSpeed(currentPhase, dmg, spd);
    }

    /// <summary>Advances the boss phase as HP crosses the 66% and 33% thresholds.</summary>
    private void OnHealthChanged(int currentHP)
    {
        if (isSpawning || isDying) return;

        float pct = (float)currentHP / maxHP;
        int targetPhase = currentPhase;

        if (pct <= 1f / 3f)
        {
            targetPhase = 3;
        }
        else if (pct <= 2f / 3f)
        {
            targetPhase = 2;
        }

        if (targetPhase != currentPhase)
        {
            currentPhase = targetPhase;
            ApplyPhaseSprite(currentPhase);
            OnPhaseChanged?.Invoke(currentPhase);
            AudioManager.Instance?.PlaySFX("sfx_boss_phase");

            if (currentPhase == 3 && !spawnedDronesPhase3)
            {
                spawnedDronesPhase3 = true;
                SpawnPhase3Drones();
            }
        }
    }

    private void ApplyPhaseSprite(int phase)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        string path = phase switch
        {
            2 => phase2SpritePath,
            3 => phase3SpritePath,
            _ => phase1SpritePath
        };
        RuntimeSpriteFixer.EnsureSprite(sr, path, true);
    }

    /// <summary>Spawns exactly two flanking drones — called once on entering Phase 3.</summary>
    private void SpawnPhase3Drones()
    {
        if (dronePrefab == null || WaveManager.Instance == null) return;

        Vector3 spawnLeft = transform.position + new Vector3(-2f, -1f, 0f);
        Vector3 spawnRight = transform.position + new Vector3(2f, -1f, 0f);

        WaveManager.Instance.SpawnEnemy(dronePrefab, spawnLeft, 1f);
        WaveManager.Instance.SpawnEnemy(dronePrefab, spawnRight, 1f);
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;
        StopAllCoroutines();

        // Immediately turn off bgm_boss and play bgm_winner
        AudioManager.Instance?.StopAllLevelSounds();
        AudioManager.Instance?.PlayBGM("bgm_winner", true);

        // Award boss points!
        if (ScoreManager.Instance != null && health != null)
        {
            ScoreManager.Instance.AddScore(health.points);
            ScoreManager.Instance.OnEnemyKilled();
        }

        // Spawn a guaranteed power-up drop for defeating the boss!
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Drop(transform.position);
        }

        // Trigger camera shake for epic boss death!
        CameraShake.Instance?.Shake(0.5f, 0.2f);

        OnBossDead?.Invoke();
        OnBossDeadGlobal?.Invoke();

        StartCoroutine(DeathSequence());
    }

    // On boss death, shove everything in range outward (P1 Stage 3 task).
    private void ApplyDeathExplosionForce()
    {
        const float radius = 5f;
        const float force = 5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            Vector2 away = (Vector2)hit.transform.position - (Vector2)transform.position;
            away = away == Vector2.zero ? Vector2.up : away.normalized;

            // The player Rigidbody2D is Kinematic, so AddForce is ignored —
            // route the push through its knockback coroutine instead.
            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                player.Knockback(away);
                continue;
            }

            Rigidbody2D body = hit.attachedRigidbody;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                body.AddForce(away * force, ForceMode2D.Impulse);
            }
        }
    }

    private IEnumerator DeathSequence()
    {
        ApplyDeathExplosionForce();

        GameObject poolObj = GameObject.Find("ExplosionLargePool");
        ObjectPool explosionPool = poolObj != null ? poolObj.GetComponent<ObjectPool>() : null;

        for (int i = 0; i < 4; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
            if (explosionPool != null)
            {
                explosionPool.Get(transform.position + randomOffset, Quaternion.identity);
            }

            AudioManager.Instance?.PlaySFX("sfx_explosion_large");
            yield return new WaitForSeconds(0.25f);
        }

        yield return new WaitForSeconds(0.5f);

        BossHUDController bossHUD = Object.FindAnyObjectByType<BossHUDController>();
        if (bossHUD != null)
        {
            bossHUD.HideBossHUD();
        }

        // Deactivate Boss object
        ObjectPool enemyPool = GetComponentInParent<ObjectPool>();
        if (enemyPool != null)
        {
            enemyPool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // Trigger Level Complete for status
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyDestroyed(gameObject);
        }
        else
        {
            LevelManager.Instance?.LevelComplete();
        }
    }
}
