using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using TMPro;

public class TitleVfx : MonoBehaviour
{
    [Header("Floating Animation")]
    public float floatAmplitude = 15f;    // Pixels to float up/down
    public float floatSpeed = 1.5f;       // Speed of floating
    public float tiltAmplitude = 2f;     // Rotation angle tilt (degrees)
    public float tiltSpeed = 1.0f;        // Speed of rocking

    [Header("Pulse Animation")]
    public float minScale = 0.97f;
    public float maxScale = 1.03f;
    public float pulseSpeed = 2f;

    [Header("Neon Sci-Fi Glow & Glitch")]
    public bool enableGlitch = true;
    public float minGlitchInterval = 2.0f;
    public float maxGlitchInterval = 6.0f;
    public Color neonColor1 = new Color(0f, 1f, 1f, 1f);   // Neon Cyan
    public Color neonColor2 = new Color(1f, 0f, 1f, 1f);   // Neon Magenta
    public Color normalColor = Color.white;

    private Vector2 originalPosition;
    private Vector3 originalScale;
    private RectTransform rectTransform;
    private TMP_Text uiText;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        uiText = GetComponent<TMP_Text>();
        
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;
        }
        else
        {
            originalScale = transform.localScale;
        }

        if (uiText != null)
        {
            normalColor = uiText.color;
        }

        if (enableGlitch)
        {
            StartCoroutine(GlitchRoutine());
        }
    }

    private void Update()
    {
        float time = Time.time;

        // 1. Floating motion (Sine wave)
        if (rectTransform != null)
        {
            float newY = originalPosition.y + Mathf.Sin(time * floatSpeed) * floatAmplitude;
            rectTransform.anchoredPosition = new Vector2(originalPosition.x, newY);
        }

        // 2. Rocking/Tilt motion (Cosine wave)
        float currentTilt = Mathf.Cos(time * tiltSpeed) * tiltAmplitude;
        transform.localRotation = Quaternion.Euler(0f, 0f, currentTilt);

        // 3. Pulsate Scale
        float scaleMultiplier = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(time * pulseSpeed) + 1f) / 2f);
        transform.localScale = originalScale * scaleMultiplier;
    }

    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            // Wait for a random interval before glitching
            yield return new WaitForSeconds(Random.Range(minGlitchInterval, maxGlitchInterval));

            int glitchFrames = Random.Range(3, 8);
            Vector2 posOffset = Vector2.zero;

            for (int i = 0; i < glitchFrames; i++)
            {
                // Jitter position
                if (rectTransform != null)
                {
                    posOffset = new Vector2(Random.Range(-8f, 8f), Random.Range(-4f, 4f));
                    rectTransform.anchoredPosition = originalPosition + posOffset;
                }

                // Rapidly cycle colors
                if (uiText != null)
                {
                    float colorRoll = Random.value;
                    if (colorRoll < 0.33f)
                        uiText.color = neonColor1;
                    else if (colorRoll < 0.66f)
                        uiText.color = neonColor2;
                    else
                        uiText.color = Color.white;
                }

                // Jitter scale slightly
                transform.localScale = originalScale * Random.Range(0.9f, 1.15f);

                // Hold frame momentarily
                yield return new WaitForSeconds(Random.Range(0.02f, 0.06f));
            }

            // Restore original state
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
            if (uiText != null)
            {
                uiText.color = normalColor;
            }
            transform.localScale = originalScale;
        }
    }
}
