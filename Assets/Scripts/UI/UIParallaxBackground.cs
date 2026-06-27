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

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialAnchoredPosition = rectTransform.anchoredPosition;
        
        // Use the height of the RectTransform for looping
        imageHeight = rectTransform.rect.height;
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
    }
}
