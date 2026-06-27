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

    private void Start()
    {
        RuntimeSpriteFixer.EnsureSprite(GetComponent<SpriteRenderer>(), "Assets/Sprites/Enemies/enemy_hunter.png");
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
        hasBeenVisible = false;
        _dying = false;
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
        float targetX = rb.position.x;
        if (player != null && player.gameObject.activeInHierarchy)
        {
            targetX = player.position.x;
        }

        float newX = Mathf.MoveTowards(rb.position.x, targetX, moveSpeed * Time.fixedDeltaTime);
        float newY = rb.position.y - verticalSpeed * Time.fixedDeltaTime;
        rb.MovePosition(new Vector2(newX, newY));
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
                bulletPool.Get(bulletSpawnPoint.position, Quaternion.identity);
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
