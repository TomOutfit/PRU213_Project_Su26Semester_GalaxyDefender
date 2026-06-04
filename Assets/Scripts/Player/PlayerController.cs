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
    public Transform firePoint;
    public float fireRate = 0.15f;
    private float nextFireTime = 0f;
    private ObjectPool playerBulletPool;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private Camera mainCamera;

    private Vector2 inputVector;
    private bool isDashing = false;
    private float lastDashTime = -10f;

    private float minX, maxX, minY, maxY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        mainCamera = Camera.main;
        UpdateScreenBounds();
    }

    private void Update()
    {
        if (isDashing) return;

        inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine());
        }

        if (Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            FireBullet();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

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

    private void FireBullet()
    {
        if (firePoint == null) return;

        if (playerBulletPool == null)
        {
            playerBulletPool = GameObject.Find("BulletPlayerPool")?.GetComponent<ObjectPool>();
        }

        if (playerBulletPool != null)
        {
            GameObject bullet = playerBulletPool.Get(firePoint.position, Quaternion.identity);
            BulletPlayer bp = bullet.GetComponent<BulletPlayer>();
            if (bp != null)
            {
                bp.pool = playerBulletPool;
            }
            AudioManager.Instance?.PlaySFX("sfx_shoot_player");
        }
    }
}
