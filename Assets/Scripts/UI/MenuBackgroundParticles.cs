using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuBackgroundParticles : MonoBehaviour
{
    [Header("Settings")]
    public Sprite particleSprite; // Optional: Assign a star sprite, otherwise a tiny white square is generated
    public float spawnRate = 0.5f; // Seconds between spawns
    public int maxParticles = 30;
    public float minSpeed = 10f;
    public float maxSpeed = 30f;
    public float minSize = 2f;
    public float maxSize = 8f;
    public float fadeDuration = 1.5f;

    [Header("Shooting Stars")]
    public float shootingStarChance = 0.15f; // Chance per spawn
    public float shootingStarSpeed = 250f;
    public float shootingStarSize = 12f;

    private Sprite generatedSprite;
    private int currentParticleCount = 0;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        // Generate a plain white 2x2 sprite if no sprite is assigned
        if (particleSprite == null)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.SetPixel(0, 1, Color.white);
            tex.SetPixel(1, 0, Color.white);
            tex.SetPixel(1, 1, Color.white);
            tex.Apply();
            generatedSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            particleSprite = generatedSprite;
        }

        // Spawn initial batch to avoid a completely empty screen at start
        int initialCount = maxParticles / 2;
        for (int i = 0; i < initialCount; i++)
        {
            SpawnParticle(true);
        }

        StartCoroutine(SpawnRoutine());
    }

    private void OnDestroy()
    {
        if (generatedSprite != null)
        {
            Destroy(generatedSprite.texture);
            Destroy(generatedSprite);
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnRate);
            if (currentParticleCount < maxParticles)
            {
                SpawnParticle(false);
            }
        }
    }

    private void SpawnParticle(bool randomizeStartPos)
    {
        bool isShootingStar = !randomizeStartPos && (Random.value < shootingStarChance);

        GameObject pGO = new GameObject(isShootingStar ? "ShootingStar" : "BackgroundStar", typeof(RectTransform), typeof(Image));
        pGO.transform.SetParent(transform, false);

        RectTransform pRect = pGO.GetComponent<RectTransform>();
        Image pImg = pGO.GetComponent<Image>();
        pImg.sprite = particleSprite;
        pImg.raycastTarget = false;

        currentParticleCount++;

        // Calculate spawn bounds based on RectTransform size
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        Vector2 spawnPos;
        float size;
        float speed;
        Vector2 direction;

        if (isShootingStar)
        {
            // Shooting stars fly diagonally downwards-left or downwards-right
            size = Random.Range(shootingStarSize * 0.7f, shootingStarSize * 1.3f);
            speed = Random.Range(shootingStarSpeed * 0.8f, shootingStarSpeed * 1.2f);
            
            // Spawn along the top or right edge
            if (Random.value > 0.5f)
                spawnPos = new Vector2(Random.Range(-width / 2f, width / 2f), height / 2f); // top
            else
                spawnPos = new Vector2(width / 2f, Random.Range(-height / 2f, height / 2f)); // right

            direction = new Vector2(Random.Range(-0.8f, -0.5f), Random.Range(-0.6f, -0.3f)).normalized;
            pImg.color = new Color(0.8f, 0.95f, 1f, 0f); // Light blue/cyan tint
        }
        else
        {
            size = Random.Range(minSize, maxSize);
            speed = Random.Range(minSpeed, maxSpeed);
            
            if (randomizeStartPos)
            {
                spawnPos = new Vector2(Random.Range(-width / 2f, width / 2f), Random.Range(-height / 2f, height / 2f));
            }
            else
            {
                // Normal stars drift slowly downwards
                spawnPos = new Vector2(Random.Range(-width / 2f, width / 2f), height / 2f + 10f);
            }

            direction = Vector2.down;
            pImg.color = new Color(1f, 1f, 1f, 0f); // Pure white, starts invisible
        }

        pRect.sizeDelta = new Vector2(size, size);
        pRect.anchoredPosition = spawnPos;

        StartCoroutine(AnimateParticle(pRect, pImg, direction, speed, isShootingStar, randomizeStartPos));
    }

    private IEnumerator AnimateParticle(RectTransform pRect, Image pImg, Vector2 direction, float speed, bool isShootingStar, bool wasRandomStart)
    {
        float lifespan = isShootingStar ? Random.Range(0.8f, 1.5f) : Random.Range(10f, 25f);
        float elapsed = 0f;

        float maxAlpha = isShootingStar ? Random.Range(0.7f, 1.0f) : Random.Range(0.2f, 0.6f);
        Color baseCol = pImg.color;

        Vector2 movement = direction * speed;

        while (elapsed < lifespan && pRect != null)
        {
            elapsed += Time.deltaTime;
            
            // Move particle
            pRect.anchoredPosition += movement * Time.deltaTime;

            // Fade in and out
            float alpha = 0f;
            if (wasRandomStart)
            {
                // Already placed on screen, slowly fade out as it reaches the end of its life
                float pct = elapsed / lifespan;
                alpha = Mathf.Lerp(maxAlpha, 0f, pct);
            }
            else
            {
                // Spawned at top edge, fade in at beginning, fade out at end
                float halfLife = lifespan * 0.5f;
                if (elapsed < fadeDuration)
                {
                    alpha = Mathf.Lerp(0f, maxAlpha, elapsed / fadeDuration);
                }
                else if (elapsed > lifespan - fadeDuration)
                {
                    alpha = Mathf.Lerp(maxAlpha, 0f, (elapsed - (lifespan - fadeDuration)) / fadeDuration);
                }
                else
                {
                    alpha = maxAlpha;
                }
            }

            baseCol.a = alpha;
            pImg.color = baseCol;

            yield return null;
        }

        if (pRect != null)
        {
            Destroy(pRect.gameObject);
        }
        currentParticleCount--;
    }
}
