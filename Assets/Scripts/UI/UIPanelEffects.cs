using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelEffects : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public bool animateScale = true;
    public Vector3 startScale = new Vector3(0.8f, 0.8f, 1f);
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine currentAnimCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Show();
    }

    public void Show()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);
        currentAnimCoroutine = StartCoroutine(AnimatePanel(0f, 1f, startScale, Vector3.one));
    }

    public void Hide(System.Action onComplete = null)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);
        currentAnimCoroutine = StartCoroutine(AnimatePanel(canvasGroup.alpha, 0f, transform.localScale, startScale, () =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    private IEnumerator AnimatePanel(float fromAlpha, float toAlpha, Vector3 fromScale, Vector3 toScale, System.Action onComplete = null)
    {
        float elapsed = 0f;
        canvasGroup.alpha = fromAlpha;
        if (animateScale && rectTransform != null)
        {
            rectTransform.localScale = fromScale;
        }

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float pct = Mathf.Clamp01(elapsed / animationDuration);
            float curvePct = animationCurve.Evaluate(pct);

            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, curvePct);
            if (animateScale && rectTransform != null)
            {
                rectTransform.localScale = Vector3.Lerp(fromScale, toScale, curvePct);
            }

            yield return null;
        }

        canvasGroup.alpha = toAlpha;
        if (animateScale && rectTransform != null)
        {
            rectTransform.localScale = toScale;
        }

        onComplete?.Invoke();
    }
}
