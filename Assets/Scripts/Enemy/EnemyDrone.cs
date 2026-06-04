using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDrone : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 2f;
    [HideInInspector]
    public float speed;

    [Header("Shooting")]
    public float fireRate = 2f;
    public Transform firePoint;

    private Rigidbody2D rb;
    private Coroutine fireRoutine;
    private ObjectPool enemyBulletPool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        speed = baseSpeed;
        if (fireRoutine != null) StopCoroutine(fireRoutine);
        fireRoutine = StartCoroutine(FireRoutine());
    }

    private void OnDisable()
    {
        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + Vector2.down * speed * Time.fixedDeltaTime);
    }

    private IEnumerator FireRoutine()
    {
        // Initial delay so not all drones shoot simultaneously on spawn
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        while (true)
        {
            Fire();
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void Fire()
    {
        if (firePoint == null) return;

        if (enemyBulletPool == null)
        {
            enemyBulletPool = GameObject.Find("BulletEnemyPool")?.GetComponent<ObjectPool>();
        }

        if (enemyBulletPool != null)
        {
            GameObject bullet = enemyBulletPool.Get(firePoint.position, Quaternion.identity);
            BulletEnemy bulletEnemy = bullet.GetComponent<BulletEnemy>();
            if (bulletEnemy != null)
            {
                bulletEnemy.pool = enemyBulletPool;
            }
        }
    }

    private void OnBecameInvisible()
    {
        if (gameObject.activeSelf)
        {
            WaveManager.Instance?.ReleaseEnemy(gameObject);
        }
    }
}
