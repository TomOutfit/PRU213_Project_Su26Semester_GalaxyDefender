using System.Collections;
using UnityEngine;

public class SpriteFlash : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void Flash(float duration = 0.1f, Color? flashColor = null)
    {
        if (spriteRenderer == null) return;
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        Color targetColor = flashColor ?? Color.red;
        flashCoroutine = StartCoroutine(DoFlash(duration, targetColor));
    }

    private IEnumerator DoFlash(float duration, Color targetColor)
    {
        spriteRenderer.color = targetColor;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}
