using UnityEngine;
using UnityEngine.UI;

public class UIParallaxBackground : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public float scrollSpeed = 50f;
    public bool scrollUp = false;

    private RectTransform rectTransform;
    private float imageHeight;
    private Vector2 initialAnchoredPosition;
    private Image backgroundImage;
    private Color baseColor;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialAnchoredPosition = rectTransform.anchoredPosition;
        
        // Use the height of the RectTransform for looping
        imageHeight = rectTransform.rect.height;

        // Cache the Image component for the ambient light cycle
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            baseColor = backgroundImage.color;
        }
    }

    private void Update()
    {
        float direction = scrollUp ? 1f : -1f;
        float moveAmount = scrollSpeed * Time.deltaTime * direction;
        
        Vector2 pos = rectTransform.anchoredPosition;
        pos.y += moveAmount;

        // Loop the position
        if (!scrollUp && pos.y <= initialAnchoredPosition.y - imageHeight)
        {
            pos.y += imageHeight;
        }
        else if (scrollUp && pos.y >= initialAnchoredPosition.y + imageHeight)
        {
            pos.y -= imageHeight;
        }

        rectTransform.anchoredPosition = pos;

        // Ambient lighting/pulsing effect
        if (backgroundImage != null)
        {
            // Breathing cycle: slow oscillation to make space feel alive
            float cycleSpeed = 0.4f;
            float sinVal = Mathf.Sin(Time.time * cycleSpeed);
            
            // Cycle hues smoothly
            float r = Mathf.Lerp(0.7f, 1.0f, (sinVal + 1f) / 2f);
            float g = Mathf.Lerp(0.6f, 0.95f, (Mathf.Cos(Time.time * 0.3f) + 1f) / 2f);
            float b = Mathf.Lerp(0.8f, 1.0f, (sinVal + 1f) / 2f);
            
            backgroundImage.color = new Color(baseColor.r * r, baseColor.g * g, baseColor.b * b, baseColor.a);
        }
    }
}
