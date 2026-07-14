using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the player ship: 8-directional movement (normalized so diagonals are not faster),
/// a Shift-triggered dash with i-frames, Space-to-shoot from a pooled bullet, and a hit
/// knockback nudge. Movement is applied to a Kinematic Rigidbody2D via MovePosition in
/// FixedUpdate and clamped to the lower half of the camera viewport.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f;
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.5f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float fireRate = 0.05f;

    [Header("Custom Ship Visuals")]
    [Tooltip("Leave empty to use default ship_player sprite")]
    public Sprite customShipSprite;
    [Tooltip("Leave empty to use default bullet_player sprite")]
    public Sprite customBulletSprite;

    [Header("Random Ship Selection")]
    [Tooltip("If checked, picks a random configuration from shipConfigurations on Start")]
    public bool useRandomShip = true;
    public ShipConfig[] shipConfigurations;

    [Header("Weapon Upgrade System")]
    public int bulletCount = 3;

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
        bulletCount = 3;
        transform.rotation = Quaternion.identity; // Point straight up
        if (useRandomShip && shipConfigurations != null && shipConfigurations.Length > 0)
        {
            int randomIndex = Random.Range(0, shipConfigurations.Length);
            ShipConfig selected = shipConfigurations[randomIndex];
            customShipSprite = selected.shipSprite;
            customBulletSprite = selected.bulletSprite;
            Debug.Log($"[PlayerController] Selected random ship: {selected.shipName}");
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (customShipSprite != null)
        {
            sr.sprite = customShipSprite;
        }
        else
        {
            RuntimeSpriteFixer.EnsureSprite(sr, "Assets/Sprites/Player/player_ship.png");
        }

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

        // Add a premium trail effect for artistic performance
        TrailRenderer trail = gameObject.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.0f;
            trail.sortingOrder = 3;
            
            // Create a gorgeous cyan to transparent gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0f, 0.8f, 1f, 1f), 0f), 
                    new GradientColorKey(new Color(0f, 0.2f, 1f, 0.4f), 0.5f),
                    new GradientColorKey(new Color(0f, 0.05f, 0.5f, 0f), 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.8f, 0f), 
                    new GradientAlphaKey(0.4f, 0.5f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            trail.colorGradient = gradient;
            
            // Set trail material using Sprites/Default shader
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                trail.material = new Material(spriteShader);
            }
        }
    }

    private void Update()
    {
        if (isDashing || isKnockback) return;

        inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        // Rotate ship to face the direction of movement smoothly
        if (inputVector != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(inputVector.y, inputVector.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine());
        }

        // Shooting logic (supports Space)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.State.Playing)
        {
            if (Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                Shoot();
            }
        }
    }

    private void LateUpdate()
    {
        if (customShipSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = customShipSprite;
            }
        }
    }

    private void Shoot()
    {
        if (bulletSpawnPoint != null)
        {
            if (bulletCount <= 1)
            {
                SpawnPlayerBullet(bulletSpawnPoint.position, transform.rotation);
                CameraShake.Instance?.Shake(0.015f, 0.04f); // Minor shake for base weapon
            }
            else
            {
                float totalArc = Mathf.Min(160f, (bulletCount - 1) * 15f);
                float startAngle = -totalArc / 2f;
                float angleStep = totalArc / (bulletCount - 1);

                for (int j = 0; j < bulletCount; j++)
                {
                    float angle = startAngle + j * angleStep;
                    SpawnPlayerBullet(bulletSpawnPoint.position, transform.rotation * Quaternion.Euler(0f, 0f, -angle));
                }

                // Shake intensity scales with bullet tier
                float shakeIntensity = bulletCount == 3 ? 0.03f : (bulletCount == 9 ? 0.06f : 0.12f);
                CameraShake.Instance?.Shake(shakeIntensity, 0.05f);
            }
        }

        AudioManager.Instance?.PlaySFX("sfx_shoot_player");
    }

    private void SpawnPlayerBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = null;
        if (bulletPool != null)
        {
            bullet = bulletPool.Get(position, rotation);
        }
        else if (bulletPrefab != null)
        {
            bullet = Instantiate(bulletPrefab, position, rotation);
        }

        if (bullet != null)
        {
            SpriteRenderer bulletSr = bullet.GetComponent<SpriteRenderer>();
            if (bulletSr != null)
            {
                if (customBulletSprite != null)
                {
                    bulletSr.sprite = customBulletSprite;
                }
                else
                {
                    RuntimeSpriteFixer.EnsureSprite(bulletSr, "Assets/Sprites/Bullets/bullet_player.png");
                }
            }
        }
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

    /// <summary>
    /// Dashes in the current input direction (or up if idle) at <see cref="dashSpeed"/> for
    /// <see cref="dashDuration"/>. Sets the dashing flag so PlayerHealth grants i-frames.
    /// </summary>
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

    /// <summary>
    /// Pushes the player away from a hit. The Rigidbody2D is Kinematic, so AddForce is ignored;
    /// instead the body is nudged via MovePosition over a short window, respecting screen clamp.
    /// </summary>
    /// <param name="dir">World-space direction to push the player toward (normalized internally).</param>
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

    public void UpgradeWeapon()
    {
        bulletCount = Mathf.Min(27, bulletCount * 3);
        Debug.Log($"[PlayerController] Weapon upgraded! Bullet count: {bulletCount}");
    }

    public void ResetWeapon()
    {
        bulletCount = 3;
        transform.rotation = Quaternion.identity; // Reset rotation to straight up
        Debug.Log("[PlayerController] Weapon reset to starting level.");
    }

    private void UpdateScreenBounds()
    {
        if (mainCamera == null) return;

        // Padding to keep the ship from going half off-screen
        float paddingX = 0.625f; 
        float paddingY = 0.625f; 

        // Player Y is clamped between bottom 10% and bottom 50% of the screen
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.1f, mainCamera.nearClipPlane));
        Vector3 middleRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, mainCamera.nearClipPlane));

        minX = bottomLeft.x + paddingX;
        maxX = middleRight.x - paddingX;
        minY = bottomLeft.y + paddingY;
        maxY = middleRight.y - paddingY;
    }

    [System.Serializable]
    public struct ShipConfig
    {
        public string shipName;
        public Sprite shipSprite;
        public Sprite bulletSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (shipConfigurations == null || shipConfigurations.Length == 0)
        {
            PopulateDefaultConfigurations();
        }
    }

    private void Reset()
    {
        PopulateDefaultConfigurations();
    }

    [ContextMenu("Populate Ship Configurations")]
    public void PopulateDefaultConfigurations()
    {
        string[] shipNames = { "Default", "Iron Vanguard", "Nova Prism", "Shadow Wraith", "Star Swift" };
        string[] shipPaths = {
            "Assets/Sprites/Player/player_ship.png",
            "Assets/Sprites/Player/iron_vanguard.png",
            "Assets/Sprites/Player/nova_prism.png",
            "Assets/Sprites/Player/shadow_wraith.png",
            "Assets/Sprites/Player/star_swift.png"
        };
        string[] bulletPaths = {
            "Assets/Sprites/Bullets/bullet_player.png",
            "Assets/Sprites/Bullets/player_iron_vanguard_bullet.png",
            "Assets/Sprites/Bullets/player_nova_prism_bullet.png",
            "Assets/Sprites/Bullets/player_shadow_wraith_bullet.png",
            "Assets/Sprites/Bullets/player_star_swift_bullet.png"
        };

        shipConfigurations = new ShipConfig[shipNames.Length];
        for (int i = 0; i < shipNames.Length; i++)
        {
            Sprite sSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(shipPaths[i]);
            Sprite bSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(bulletPaths[i]);
            shipConfigurations[i] = new ShipConfig
            {
                shipName = shipNames[i],
                shipSprite = sSprite,
                bulletSprite = bSprite
            };
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[PlayerController] Auto-populated 5 ship configurations!");
    }
#endif
}
