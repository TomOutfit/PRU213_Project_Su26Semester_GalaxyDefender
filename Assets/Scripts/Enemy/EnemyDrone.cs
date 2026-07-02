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
        "Assets/Sprites/Enemies/enemy_hunter.png",
        "Assets/Sprites/Enemies/enemy_aegis_guardian.png",
        "Assets/Sprites/Enemies/enemy_harvester_curved.png",
        "Assets/Sprites/Enemies/enemy_pulse_ray.png",
        "Assets/Sprites/Enemies/enemy_void_stinger.png"
    };

    private static readonly int[] ENEMY_POINTS = new int[]
    {
        500, // drone
        1500, // hunter
        2000, // aegis_guardian
        1000, // harvester_curved
        2500, // pulse_ray
        3000  // void_stinger
    };

    private static readonly string[] BULLET_SPRITES = new string[]
    {
        "Assets/Sprites/Bullets/bullet_enemy.png",
        "Assets/Sprites/Bullets/enemy_red_energy_spike.png",
        "Assets/Sprites/Bullets/enemy_teal_energy_orb.png",
        "Assets/Sprites/Bullets/enemy_purple_bio-spore.png",
        "Assets/Sprites/Bullets/enemy_cyan_sniper_beam.png",
        "Assets/Sprites/Bullets/enemy_red_energy_spike.png"
    };

    private int currentSpriteIndex = 0;

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
                health.points = ENEMY_POINTS[index];
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
        rb.MovePosition(rb.position + Vector2.down * moveSpeed * Time.fixedDeltaTime);
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

