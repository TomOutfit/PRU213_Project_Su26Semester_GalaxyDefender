using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 2.0f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float fireRate = 0.15f;

    [Header("Knockback")]
    public float knockbackSpeed = 8f;
    public float knockbackDuration = 0.1f;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private Camera mainCamera;

    private Vector2 inputVector;
    private bool isDashing = false;
    private bool isKnockback = false;
    private float lastDashTime = -10f;

    private ObjectPool bulletPool;
    private float nextFireTime = 0f;

    private float minX, maxX, minY, maxY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        mainCamera = Camera.main;
        UpdateScreenBounds();
    }

    private void Start()
    {
        RuntimeSpriteFixer.EnsureSprite(GetComponent<SpriteRenderer>(), "Assets/Sprites/Player/player_ship.png");
        Transform thruster = transform.Find("Thruster");
        if (thruster != null)
        {
            RuntimeSpriteFixer.EnsureSprite(thruster.GetComponent<SpriteRenderer>(), "Assets/Sprites/Player/player_thruster_sheet.png");
        }
        GameObject poolObj = GameObject.Find("BulletPlayerPool");
        if (poolObj != null)
        {
            bulletPool = poolObj.GetComponent<ObjectPool>();
        }
    }

    private void Update()
    {
        if (isDashing || isKnockback) return;

        inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine());
        }

        // Shooting logic
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.State.Playing)
        {
            if (Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                Shoot();
            }
        }
    }

    private void Shoot()
    {
        if (bulletPool != null && bulletSpawnPoint != null)
        {
            bulletPool.Get(bulletSpawnPoint.position, Quaternion.identity);
        }
        else if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        }

        AudioManager.Instance?.PlaySFX("sfx_shoot_player");
    }

    private void FixedUpdate()
    {
        if (isDashing || isKnockback) return;

        UpdateScreenBounds(); // Called to ensure bounds are updated if screen resolution changes
        Vector2 targetPosition = rb.position + inputVector * moveSpeed * Time.fixedDeltaTime;
        
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        rb.MovePosition(targetPosition);
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        if (playerHealth != null) playerHealth.isDashing = true;
        
        lastDashTime = Time.time;

        // Use the current input vector or default to up if no input
        Vector2 dashDirection = inputVector != Vector2.zero ? inputVector : Vector2.up;

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            Vector2 dashPos = rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime;
            dashPos.x = Mathf.Clamp(dashPos.x, minX, maxX);
            dashPos.y = Mathf.Clamp(dashPos.y, minY, maxY);
            
            rb.MovePosition(dashPos);
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        if (playerHealth != null) playerHealth.isDashing = false;
    }

    // Pushes the player away from a hit. The Rigidbody2D is Kinematic, so AddForce is ignored;
    // instead we nudge the body via MovePosition over a short window, respecting screen clamp.
    public void Knockback(Vector2 dir)
    {
        if (dir == Vector2.zero || isDashing) return;
        StartCoroutine(KnockbackRoutine(dir.normalized));
    }

    private IEnumerator KnockbackRoutine(Vector2 dir)
    {
        isKnockback = true;

        float startTime = Time.time;
        while (Time.time < startTime + knockbackDuration)
        {
            UpdateScreenBounds();
            Vector2 pos = rb.position + dir * knockbackSpeed * Time.fixedDeltaTime;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }

        isKnockback = false;
    }

    private void UpdateScreenBounds()
    {
        if (mainCamera == null) return;

        // Padding to keep the ship from going half off-screen
        float paddingX = 0.5f; 
        float paddingY = 0.5f; 

        // Player Y is clamped between bottom 10% and bottom 50% of the screen
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.1f, mainCamera.nearClipPlane));
        Vector3 middleRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, mainCamera.nearClipPlane));

        minX = bottomLeft.x + paddingX;
        maxX = middleRight.x - paddingX;
        minY = bottomLeft.y + paddingY;
        maxY = middleRight.y - paddingY;
    }
}
