using UnityEngine;

public class BulletPlayer : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 12f;
    public int damage = 10;

    [HideInInspector]
    public ObjectPool pool;

    private void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") || collision.CompareTag("Boss"))
        {
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            Release();
        }
    }

    private void Release()
    {
        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        Release();
    }
}
