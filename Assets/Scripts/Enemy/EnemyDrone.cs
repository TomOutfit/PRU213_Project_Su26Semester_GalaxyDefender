using System.Collections;
using UnityEngine;

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

    private void Start()
    {
        RuntimeSpriteFixer.EnsureSprite(GetComponent<SpriteRenderer>(), "Assets/Sprites/Enemies/enemy_drone.png");
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

    private IEnumerator FireRoutine()
    {
        // Random offset so they don't all shoot on the exact same frame
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

