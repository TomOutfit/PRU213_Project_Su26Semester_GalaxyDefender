using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton that handles cinematic fade-out → load → fade-in transitions between scenes.
/// Spawns its own full-screen Canvas at runtime — no prefab needed.
/// Persists across all scenes via DontDestroyOnLoad.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [Tooltip("Duration of the fade-out (current scene goes dark).")]
    public float fadeOutDuration = 0.5f;
    [Tooltip("Duration of the fade-in (new scene reveals from dark).")]
    public float fadeInDuration = 0.7f;
    [Tooltip("Color of the transition overlay (black recommended).")]
    public Color fadeColor = Color.black;

    // Internal overlay canvas
    private Canvas        _overlayCanvas;
    private CanvasGroup   _overlayGroup;
    private Image         _overlayImage;
    private bool          _isTransitioning = false;

    // ─────────────────────────────────────────────
    #region Unity Lifecycle
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
        // If we were JUST created in a scene that is already running (e.g. bootstrapped
        // from MainMenu), OnSceneLoaded will never fire for this scene.
        // Detect that: if overlay is fully opaque and no transition is in progress, fade in now.
        if (!_isTransitioning && _overlayGroup != null && _overlayGroup.alpha >= 1f)
        {
            StartCoroutine(FadeInAfterLoad());
        }
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    #endregion

    // ─────────────────────────────────────────────
    #region Public API

    /// <summary>Load a scene by name with a cinematic fade transition.</summary>
    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    /// <summary>Convenience overload: load by build index.</summary>
    public void LoadScene(int buildIndex)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(buildIndex));
    }

    #endregion

    // ─────────────────────────────────────────────
    #region Transition Coroutines

    private IEnumerator TransitionRoutine(string sceneName)
    {
        _isTransitioning = true;

        yield return StartCoroutine(FadeTo(1f, fadeOutDuration)); // Fade out current scene

        // Async load the next scene in the background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Wait until fully loaded (progress reaches 0.9 = unity quirk)
        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;

        // Wait one frame for the scene to fully activate
        yield return null;
        yield return null;
        // Fade-in is handled in OnSceneLoaded
    }

    private IEnumerator TransitionRoutine(int buildIndex)
    {
        _isTransitioning = true;

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
            elapsed += Time.unscaledDeltaTime; // Use unscaled so it works even when paused
            _overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        _overlayGroup.alpha = targetAlpha;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only fade in if WE triggered this scene load via a transition.
        // If _isTransitioning is false, it means some other code loaded the scene
        // (or we were just bootstrapped), so Start() already handles the fade-in.
        if (_isTransitioning)
        {
            StartCoroutine(FadeInAfterLoad());
        }
    }

    private IEnumerator FadeInAfterLoad()
    {
        // Ensure overlay is fully opaque at start of new scene
        _overlayGroup.alpha = 1f;

        // Small delay so scene's Start() can run before we start revealing
        yield return new WaitForSecondsRealtime(0.1f);

        yield return StartCoroutine(FadeTo(0f, fadeInDuration));
        _isTransitioning = false;
    }

    #endregion

    // ─────────────────────────────────────────────
    #region Canvas Setup

    private void BuildOverlayCanvas()
    {
        // Root GameObject for the overlay
        GameObject canvasGO = new GameObject("[SceneTransitionOverlay]");
        canvasGO.transform.SetParent(transform, false);

        _overlayCanvas = canvasGO.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = 9999; // Always on top of everything

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full-screen black panel
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

        // CanvasGroup controls the alpha
        _overlayGroup = canvasGO.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 1f; // Start fully black (will fade in on first scene load)
        _overlayGroup.interactable = false;
        _overlayGroup.blocksRaycasts = false;
    }

    #endregion
}
