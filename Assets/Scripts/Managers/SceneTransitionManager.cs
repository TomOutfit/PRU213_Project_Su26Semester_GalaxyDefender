using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    public float fadeOutDuration = 0.4f;
    public float fadeInDuration = 0.6f;
    public Color fadeColor = new Color(0.02f, 0.02f, 0.05f, 1f); // Deep dark blue

    private Canvas _overlayCanvas;
    private CanvasGroup _overlayGroup;
    private Image _overlayImage;
    private TextMeshProUGUI _loadingText;
    private bool _isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlayCanvas();
    }

    private void Start()
    {
        if (!_isTransitioning && _overlayGroup != null && _overlayGroup.alpha >= 1f)
        {
            StartCoroutine(FadeInAfterLoad());
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    public void LoadScene(int buildIndex)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(buildIndex));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        _isTransitioning = true;
        if (_loadingText != null) _loadingText.gameObject.SetActive(true);

        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;
        yield return null;
        yield return null;
    }

    private IEnumerator TransitionRoutine(int buildIndex)
    {
        _isTransitioning = true;
        if (_loadingText != null) _loadingText.gameObject.SetActive(true);

        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(buildIndex);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;
        yield return null;
        yield return null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = _overlayGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        _overlayGroup.alpha = targetAlpha;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_loadingText != null) _loadingText.gameObject.SetActive(false);
        if (_isTransitioning)
        {
            StartCoroutine(FadeInAfterLoad());
        }
    }

    private IEnumerator FadeInAfterLoad()
    {
        _overlayGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(0.2f);
        yield return StartCoroutine(FadeTo(0f, fadeInDuration));
        _isTransitioning = false;
    }

    private void BuildOverlayCanvas()
    {
        GameObject canvasGO = new GameObject("[SceneTransitionOverlay]");
        canvasGO.transform.SetParent(transform, false);

        _overlayCanvas = canvasGO.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = 9999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("FadePanel");
        imgGO.transform.SetParent(canvasGO.transform, false);

        _overlayImage = imgGO.AddComponent<Image>();
        _overlayImage.color = fadeColor;
        _overlayImage.raycastTarget = false;

        RectTransform rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Add Loading Text
        GameObject textGO = new GameObject("LoadingText");
        textGO.transform.SetParent(canvasGO.transform, false);
        _loadingText = textGO.AddComponent<TextMeshProUGUI>();
        _loadingText.text = "ESTABLISHING LINK...";
        _loadingText.fontSize = 28;
        _loadingText.alignment = TextAlignmentOptions.Center;
        _loadingText.color = new Color(0, 1, 1, 0.7f);
        _loadingText.gameObject.SetActive(false);

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = new Vector2(0, 0);

        _overlayGroup = canvasGO.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 1f;
        _overlayGroup.interactable = false;
        _overlayGroup.blocksRaycasts = false;
    }
}
