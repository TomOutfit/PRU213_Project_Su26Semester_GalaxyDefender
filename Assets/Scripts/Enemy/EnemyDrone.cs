using System.Collections;
using UnityEngine;

/// <summary>
/// Basic enemy: drifts straight down at <see cref="moveSpeed"/> and fires a pooled bullet every
/// <see cref="fireInterval"/> seconds (with a small random offset so a wave doesn't fire in
/// lockstep). Returns to its pool once it scrolls off-screen.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDrone : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float fireInterval = 2f;
    public Transform bulletSpawnPoint;
    
    private Rigidbody2D rb;
    private ObjectPool bulletPool;
    private ObjectPool enemyPool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private bool hasBeenVisible = false;
    private bool _dying = false;

    private static readonly string[] ENEMY_SPRITES = new string[]
    {
        "Assets/Sprites/Enemies/enemy_drone.png",
        "Assets/Sprites/Enemies/enemy_aegis_guardian.png",
        "Assets/Sprites/Enemies/enemy_harvester_curved.png"
    };

    private static readonly int[] ENEMY_POINTS = new int[]
    {
        10000, // drone
        30000, // aegis_guardian
        15000  // harvester_curved
    };

    private static readonly string[] BULLET_SPRITES = new string[]
    {
        "Assets/Sprites/Bullets/bullet_enemy.png",
        "Assets/Sprites/Bullets/enemy_teal_energy_orb.png",
        "Assets/Sprites/Bullets/enemy_purple_bio-spore.png"
    };

    private int currentSpriteIndex = 0;
    private float startX;
    private float randomPhase;
    private float swayFrequency;
    private float swayAmplitude;

    private void Start()
    {
        enemyPool = GetComponentInParent<ObjectPool>();
        
        // Find the specific pool for Enemy Bullets (as dictated by P2's setup)
        GameObject poolObj = GameObject.Find("BulletEnemyPool");
        if (poolObj != null)
        {
            bulletPool = poolObj.GetComponent<ObjectPool>();
        }
    }

    private void OnEnable()
    {
        startX = rb != null ? rb.position.x : transform.position.x;
        randomPhase = Random.Range(0f, Mathf.PI * 2f);
        swayFrequency = Random.Range(1.5f, 3.5f);
        swayAmplitude = Random.Range(1.0f, 2.5f);

        hasBeenVisible = false;
        _dying = false;
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            int index = Random.Range(0, ENEMY_SPRITES.Length);
            currentSpriteIndex = index;
            RuntimeSpriteFixer.EnsureSprite(sr, ENEMY_SPRITES[index], true);
            
            EnemyHealth health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ResetHealth(20000, ENEMY_POINTS[index]);
            }
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
        float newX = startX + Mathf.Sin(Time.time * swayFrequency + randomPhase) * swayAmplitude;
        float newY = rb.position.y - moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(new Vector2(newX, newY));

        // Tilt based on horizontal movement direction
        float deltaX = newX - prevX;
        float tiltAngle = -deltaX * 15f / (swayAmplitude * swayFrequency * Time.fixedDeltaTime + 0.001f);
        tiltAngle = Mathf.Clamp(tiltAngle, -25f, 25f);
        transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle);
    }

    /// <summary>Fires a pooled enemy bullet on a fixed interval for the drone's lifetime.</summary>
    private IEnumerator FireRoutine()
    {
        // Random offset so they don't all shoot on the exact same frame
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

