using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

using TMPro;

public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scaling")]
    public float hoverScale = 1.08f;
    public float clickScale = 0.92f;
    public float transitionSpeed = 10f;

    [Header("Colors")]
    public Color hoverTextColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan
    private Color originalTextColor;
    private bool hasText = false;
    private TMP_Text textComponent;

    [Header("Audio")]
    public string hoverSFX = ""; // Safe to leave empty, can assign e.g. "sfx_player_hit" (quieter) or custom
    public string clickSFX = "sfx_shoot_player"; // Satisfying click laser sound for space shooter!

    [Header("Visual Effects")]
    public bool enableGlint = true;
    public Color glintColor = new Color(1f, 1f, 1f, 0.4f);
    public float glintDuration = 0.4f;

    [Header("Particles")]
    public bool enableClickParticles = true;
    public Color particleColor = new Color(0f, 1f, 1f, 0.8f); // Cyan sparks
    public int particleCount = 12;

    private Vector3 targetScale = Vector3.one;
    private Coroutine glintCoroutine;
    private RectTransform rectTransform;
    private Image buttonImage;
    private Sprite whitePixelSprite;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();

        // Find child text component
        textComponent = GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            originalTextColor = textComponent.color;
            hasText = true;
        }

        // Add RectMask2D dynamically if glint is enabled, to keep it within the button boundary
        if (enableGlint && GetComponent<RectMask2D>() == null && GetComponent<Mask>() == null)
        {
            gameObject.AddComponent<RectMask2D>();
        }

        // Generate a 2x2 white texture for glint & particles
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.SetPixel(0, 1, Color.white);
        tex.SetPixel(1, 0, Color.white);
        tex.SetPixel(1, 1, Color.white);
        tex.Apply();
        whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private void OnDestroy()
    {
        if (whitePixelSprite != null)
        {
            Destroy(whitePixelSprite.texture);
            Destroy(whitePixelSprite);
        }
    }

    private void Update()
    {
        // Smoothly scale the button
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = new Vector3(hoverScale, hoverScale, 1f);

        if (hasText && textComponent != null)
        {
            textComponent.color = hoverTextColor;
        }

        if (enableGlint)
        {
            if (glintCoroutine != null) StopCoroutine(glintCoroutine);
            glintCoroutine = StartCoroutine(TriggerGlint());
        }

        if (!string.IsNullOrEmpty(hoverSFX))
        {
            AudioManager.Instance?.PlaySFX(hoverSFX);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;

        if (hasText && textComponent != null)
        {
            textComponent.color = originalTextColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = new Vector3(clickScale, clickScale, 1f);

        if (!string.IsNullOrEmpty(clickSFX))
        {
            AudioManager.Instance?.PlaySFX(clickSFX);
        }

        if (enableClickParticles)
        {
            SpawnClickParticles(eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = new Vector3(hoverScale, hoverScale, 1f);
    }

    private IEnumerator TriggerGlint()
    {
        // Create glint GameObject
        GameObject glintGO = new GameObject("GlintBar", typeof(RectTransform), typeof(Image));
        glintGO.transform.SetParent(transform, false);

        RectTransform glintRect = glintGO.GetComponent<RectTransform>();
        Image glintImg = glintGO.GetComponent<Image>();

        glintImg.sprite = whitePixelSprite;
        glintImg.color = glintColor;
        glintImg.raycastTarget = false;

        // Set glint size: high, but narrow bar angled slightly
        float buttonHeight = rectTransform.rect.height;
        float buttonWidth = rectTransform.rect.width;

        glintRect.sizeDelta = new Vector2(buttonWidth * 0.25f, buttonHeight * 2f);
        glintRect.rotation = Quaternion.Euler(0f, 0f, 25f); // Angled glint

        float startX = -buttonWidth * 0.8f;
        float endX = buttonWidth * 0.8f;

        glintRect.anchoredPosition = new Vector2(startX, 0f);

        float elapsed = 0f;
        while (elapsed < glintDuration && glintRect != null)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / glintDuration;
            // Move glint across button
            float currentX = Mathf.Lerp(startX, endX, pct);
            glintRect.anchoredPosition = new Vector2(currentX, 0f);
            yield return null;
        }

        if (glintRect != null)
        {
            Destroy(glintRect.gameObject);
        }
    }

    private void SpawnClickParticles(Vector2 screenMousePos)
    {
        // Find parent Canvas to attach particles to
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Convert screen mouse position to local point in canvas RectTransform
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenMousePos, canvas.worldCamera, out localPoint);

        for (int i = 0; i < particleCount; i++)
        {
            GameObject sparkGO = new GameObject("ClickSpark", typeof(RectTransform), typeof(Image));
            sparkGO.transform.SetParent(canvasRect.transform, false);

            RectTransform sparkRect = sparkGO.GetComponent<RectTransform>();
            sparkRect.anchoredPosition = localPoint;

            float size = Random.Range(4f, 10f);
            sparkRect.sizeDelta = new Vector2(size, size);

            Image sparkImg = sparkGO.GetComponent<Image>();
            sparkImg.sprite = whitePixelSprite;
            sparkImg.color = particleColor;
            sparkImg.raycastTarget = false;

            // Random direction and velocity
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(100f, 250f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            StartCoroutine(AnimateSpark(sparkRect, sparkImg, velocity));
        }
    }

    private IEnumerator AnimateSpark(RectTransform sparkRect, Image sparkImg, Vector2 velocity)
    {
        float duration = Random.Range(0.4f, 0.7f);
        float elapsed = 0f;

        Vector3 startScale = sparkRect.localScale;
        Color startCol = sparkImg.color;

        while (elapsed < duration && sparkRect != null)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;

            // Move particle
            sparkRect.anchoredPosition += velocity * Time.deltaTime;
            // Apply slight gravity/drift
            velocity.y -= 150f * Time.deltaTime;

            // Shrink and fade
            sparkRect.localScale = Vector3.Lerp(startScale, Vector3.zero, pct);
            sparkImg.color = Color.Lerp(startCol, new Color(startCol.r, startCol.g, startCol.b, 0f), pct);

            yield return null;
        }

        if (sparkRect != null)
        {
            Destroy(sparkRect.gameObject);
        }
    }
}
