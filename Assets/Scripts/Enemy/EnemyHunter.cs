using System.Collections;
using UnityEngine;

/// <summary>
/// Aggressive enemy that tracks the player. Each FixedUpdate it eases its X toward the player at
/// <see cref="moveSpeed"/> while drifting down at <see cref="verticalSpeed"/>, and fires a pooled
/// bullet every <see cref="fireInterval"/> seconds. Returns to its pool once it scrolls off-screen.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHunter : MonoBehaviour
{
    public float moveSpeed = 3.5f;       // X-axis tracking speed
    public float verticalSpeed = 1.0f;   // Y-axis drift speed
    public float fireInterval = 1.5f;
    public Transform bulletSpawnPoint;

    private Rigidbody2D rb;
    private Transform player;
    private ObjectPool bulletPool;
    private ObjectPool enemyPool;
    private bool hasBeenVisible = false;
    private bool _dying = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private static readonly string[] ENEMY_SPRITES = new string[]
    {
        "Assets/Sprites/Enemies/enemy_hunter.png",
        "Assets/Sprites/Enemies/enemy_pulse_ray.png",
        "Assets/Sprites/Enemies/enemy_void_stinger.png"
    };

    private static readonly int[] ENEMY_POINTS = new int[]
    {
        25000, // hunter
        50000, // pulse_ray
        60000  // void_stinger
    };

    private static readonly string[] BULLET_SPRITES = new string[]
    {
        "Assets/Sprites/Bullets/enemy_red_energy_spike.png",
        "Assets/Sprites/Bullets/enemy_cyan_sniper_beam.png",
        "Assets/Sprites/Bullets/enemy_red_energy_spike.png"
    };

    private int currentSpriteIndex = 0;
    private float randomPhase;
    private float swoopFrequency;
    private float swoopAmplitude;

    private void Start()
    {
        enemyPool = GetComponentInParent<ObjectPool>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        GameObject poolObj = GameObject.Find("BulletEnemyPool");
        if (poolObj != null)
        {
            bulletPool = poolObj.GetComponent<ObjectPool>();
        }
    }

    private void OnEnable()
    {
        randomPhase = Random.Range(0f, Mathf.PI * 2f);
        swoopFrequency = Random.Range(2.0f, 4.0f);
        swoopAmplitude = Random.Range(1.0f, 2.0f);

        hasBeenVisible = false;
        _dying = false;
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Animator anim = GetComponent<Animator>();
        if (sr != null)
        {
            int index = Random.Range(0, ENEMY_SPRITES.Length);
            currentSpriteIndex = index;
            string chosenPath = ENEMY_SPRITES[index];
            RuntimeSpriteFixer.EnsureSprite(sr, chosenPath, true);
            
            if (anim != null)
            {
                anim.enabled = chosenPath.Contains("enemy_hunter.png");
            }

            EnemyHealth health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ResetHealth(30000, ENEMY_POINTS[index]);
            }
        }

        // Dynamically find player in case of respawn
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        StartCoroutine(FireRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void FixedUpdate()
    {
        float prevX = rb.position.x;
        float targetX = rb.position.x;
        if (player != null && player.gameObject.activeInHierarchy)
        {
            targetX = player.position.x;
        }

        // Swoop motion: add a sine wave offset relative to the tracking direction
        float swoopOffset = Mathf.Sin(Time.time * swoopFrequency + randomPhase) * swoopAmplitude;
        float baseNewX = Mathf.MoveTowards(rb.position.x, targetX, moveSpeed * Time.fixedDeltaTime);
        float newX = baseNewX + swoopOffset;

        float newY = rb.position.y - verticalSpeed * Time.fixedDeltaTime;
        rb.MovePosition(new Vector2(newX, newY));

        // Tilt based on horizontal movement direction
        float deltaX = newX - prevX;
        float tiltAngle = -deltaX * 20f / (moveSpeed * Time.fixedDeltaTime + 0.001f);
        tiltAngle = Mathf.Clamp(tiltAngle, -30f, 30f);
        transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle);
    }

    /// <summary>Fires a pooled enemy bullet on a fixed interval for the hunter's lifetime.</summary>
    private IEnumerator FireRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));
        while (true)
        {
            yield return new WaitForSeconds(fireInterval);
            if (bulletPool != null && bulletSpawnPoint != null)
            {
                GameObject bulletObj = bulletPool.Get(bulletSpawnPoint.position, Quaternion.identity);
                if (bulletObj != null)
                {
                    BulletEnemy bullet = bulletObj.GetComponent<BulletEnemy>();
                    if (bullet != null)
                    {
                        bullet.SetSpritePath(BULLET_SPRITES[currentSpriteIndex]);
                    }
                }
            }
        }
    }

    private void OnBecameVisible()
    {
        hasBeenVisible = true;
    }

    private void OnBecameInvisible()
    {
        if (!hasBeenVisible) return;
        if (_dying) return; // Already dying via EnemyHealth.Die()

        _dying = true;
        WaveManager.Instance?.OnEnemyDestroyed(gameObject);

        if (enemyPool != null)
        {
            enemyPool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
