using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý chuyển Scene với hiệu ứng cinematic:
/// - Fade In / Out mượt mà (SmoothStep)
/// - Hiển thị ảnh nền cốt truyện (bg_story_*) tương ứng theo scene đích
/// - Hiệu ứng đánh máy narrative + progress bar ký tự ASCII (Kích thước chữ tối giản ~30%)
/// - Vignette overlay tạo chiều sâu điện ảnh
/// - Sử dụng font Kenney Space và căn chỉnh vị trí chữ tối ưu cho từng ảnh nền
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    // INSPECTOR SETTINGS
    // ──────────────────────────────────────────────

    [Header("Transition Timing")]
    public float fadeOutDuration = 0.5f;
    public float fadeInDuration  = 0.8f;

    [Header("Color Palette")]
    public Color fallbackBgColor   = new Color(0.01f, 0.01f, 0.03f, 1f); // Đen xanh thẳm
    public Color accentCyberColor  = new Color(0.0f,  1.0f,  0.9f,  1f); // Neon Cyan

    [Header("Story Backgrounds")]
    [Tooltip("Drag: Assets/Sprites/Backgrounds/bg_story_l1")]
    [SerializeField] private Sprite _bgLevel1;
    [Tooltip("Drag: Assets/Sprites/Backgrounds/bg_story_l2")]
    [SerializeField] private Sprite _bgLevel2;
    [Tooltip("Drag: Assets/Sprites/Backgrounds/bg_story_l3")]
    [SerializeField] private Sprite _bgLevel3;
    [Tooltip("Drag: Assets/Sprites/Backgrounds/bg_story_victory")]
    [SerializeField] private Sprite _bgVictory;
    [Tooltip("Drag: Assets/Sprites/Backgrounds/bg_story_gameover")]
    [SerializeField] private Sprite _bgGameOver;

    [Header("Font")]
    [Tooltip("Drag: Assets/Fonts/Kenny_Space/Kenney Space SDF")]
    [SerializeField] private TMP_FontAsset _narrativeFont;

    // ──────────────────────────────────────────────
    // PRIVATE RUNTIME REFERENCES
    // ──────────────────────────────────────────────

    private Canvas             _canvas;
    private CanvasGroup        _group;
    private Image              _bgImage;       // Ảnh nền cốt truyện
    private Image              _vignetteImage; // Lớp tối rìa màn hình
    private TextMeshProUGUI   _narrativeText; // Chữ cốt truyện typewriter
    private TextMeshProUGUI   _statusText;    // Progress bar / trạng thái hệ thống
    private Sprite             _vignetteSprite;

    private bool _isTransitioning = false;

    // ──────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ValidateSprites();
        BuildOverlayCanvas();
    }

    /// <summary>Log cảnh báo nếu sprite nào chưa được gán trong Inspector.</summary>
    private void ValidateSprites()
    {
        int missing = 0;
        if (_bgLevel1   == null) { Debug.LogWarning("[SceneTransitionManager] bgLevel1 not assigned.");   missing++; }
        if (_bgLevel2   == null) { Debug.LogWarning("[SceneTransitionManager] bgLevel2 not assigned.");   missing++; }
        if (_bgLevel3   == null) { Debug.LogWarning("[SceneTransitionManager] bgLevel3 not assigned.");   missing++; }
        if (_bgVictory  == null) { Debug.LogWarning("[SceneTransitionManager] bgVictory not assigned.");  missing++; }
        if (_bgGameOver == null) { Debug.LogWarning("[SceneTransitionManager] bgGameOver not assigned."); missing++; }
        if (_narrativeFont == null) Debug.LogWarning("[SceneTransitionManager] narrativeFont not assigned — falling back to default TMP font.");

        if (missing == 0)
            Debug.Log("[SceneTransitionManager] All 5 story backgrounds assigned.");
    }

    private void Start()
    {
        // Fade in ngay khi scene đầu tiên load xong
        if (!_isTransitioning && _group != null && _group.alpha >= 1f)
            StartCoroutine(FadeInAfterLoad());
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // ──────────────────────────────────────────────
    // PUBLIC API
    // ──────────────────────────────────────────────

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(Transition(sceneName, () => SceneManager.LoadSceneAsync(sceneName)));
    }

    public void LoadScene(int buildIndex)
    {
        if (_isTransitioning) return;
        string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        StartCoroutine(Transition(name, () => SceneManager.LoadSceneAsync(buildIndex)));
    }

    // ──────────────────────────────────────────────
    // TRANSITION PIPELINE
    // ──────────────────────────────────────────────

    private IEnumerator Transition(string sceneName, System.Func<AsyncOperation> loadAction)
    {
        _isTransitioning = true;

        string narrative = GetNarrative(sceneName);
        Sprite bgSprite  = GetBackground(sceneName);

        // ① Ẩn tất cả HUD Canvas của scene hiện tại
        HideSceneCanvases();

        // ② Áp dụng ảnh nền tương ứng với scene sắp vào
        ApplyBackground(bgSprite);

        // ③ Cấu hình vị trí/căn lề chữ dựa trên bố cục của từng ảnh nền
        ConfigureTextLayout(sceneName);

        // ④ Hiện UI, sau đó fade ra đen phủ lên scene cũ
        ShowUI(true);
        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));

        // ⑤ Typewriter: kể câu chuyện cho người chơi đọc
        yield return StartCoroutine(TypeWriterEffect(narrative));

        // ⑥ Load scene ngầm, hiển thị thanh tiến trình ASCII
        AsyncOperation op = loadAction.Invoke();
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            SetStatus($"DOWNLOADING DATA REPOSITORIES [{Mathf.RoundToInt(op.progress * 100f)}%]\n" +
                      DrawProgressBar(op.progress));
            yield return null;
        }

        SetStatus("SYNCHRONIZATION COMPLETE. READY TO EMERGE.");
        yield return new WaitForSecondsRealtime(0.5f);

        // ⑦ Kích hoạt scene mới
        op.allowSceneActivation = true;
        yield return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isTransitioning)
        {
            // Ẩn ngay HUD mới (chưa cần hiện trong khi overlay đang phủ)
            HideSceneCanvases();
            StartCoroutine(FadeInAfterLoad());
        }
    }

    private IEnumerator FadeInAfterLoad()
    {
        _group.alpha = 1f;
        SetStatus("ESTABLISHING SECTOR CONNECTIONS...");

        yield return new WaitForSecondsRealtime(0.4f);

        ShowUI(false);
        yield return StartCoroutine(FadeTo(0f, fadeInDuration));

        // Hiện lại tất cả HUD Canvas của scene mới sau khi fade-in xong
        ShowSceneCanvases();

        _isTransitioning = false;
    }

    // ──────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────

    private void ApplyBackground(Sprite sprite)
    {
        if (_bgImage == null) return;
        if (sprite != null)
        {
            _bgImage.sprite = sprite;
            _bgImage.color  = Color.white;
            _bgImage.preserveAspect = false;
        }
        else
        {
            _bgImage.sprite = null;
            _bgImage.color  = fallbackBgColor;
        }
    }

    private void ShowUI(bool visible)
    {
        _narrativeText?.gameObject.SetActive(visible);
        _statusText?.gameObject.SetActive(visible);
        if (!visible && _narrativeText != null) _narrativeText.text = "";
    }

    private void SetStatus(string text)
    {
        if (_statusText != null) _statusText.text = text.ToUpper();
    }

    // ──────────────────────────────────────────────
    // HUD CANVAS HIDE / SHOW
    // ──────────────────────────────────────────────

    /// <summary>
    /// Ẩn tất cả Canvas trong scene hiện tại, trừ transition overlay.
    /// </summary>
    private void HideSceneCanvases()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>())
        {
            // Bỏ qua chính overlay transition của SceneTransitionManager
            if (c == _canvas || (c.transform.parent != null && c.transform.IsChildOf(transform)))
                continue;
            c.enabled = false;
        }
    }

    /// <summary>
    /// Hiện lại tất cả Canvas trong scene mới (sau khi fade-in xong).
    /// </summary>
    private void ShowSceneCanvases()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>())
        {
            if (c == _canvas || (c.transform.parent != null && c.transform.IsChildOf(transform)))
                continue;
            c.enabled = true;
        }
    }

    // ──────────────────────────────────────────────
    // TEXT CONFIGURATION PER BACKGROUND
    // ──────────────────────────────────────────────

    private void ConfigureTextLayout(string sceneName)
    {
        if (_narrativeText == null) return;

        RectTransform rt = _narrativeText.GetComponent<RectTransform>();
        if (rt == null) return;

        if (sceneName.Contains("Level1"))
        {
            // Level 1: Chữ ở phía trên màn hình (vùng vortex tối) để không đè tàu bay và console ở dưới
            rt.anchorMin = new Vector2(0.12f, 0.70f);
            rt.anchorMax = new Vector2(0.88f, 0.92f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Center;
        }
        else if (sceneName.Contains("Level2"))
        {
            // Level 2: Chữ ở cột bên trái (vùng thiên thạch tối) tránh đè tàu bay ở bên phải
            rt.anchorMin = new Vector2(0.08f, 0.25f);
            rt.anchorMax = new Vector2(0.45f, 0.75f);
            rt.pivot = new Vector2(0.0f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else if (sceneName.Contains("Level3"))
        {
            // Level 3: Chữ ở góc dưới bên trái để tránh đè trùm khổng lồ ở trên/giữa và tàu ta góc dưới-phải
            rt.anchorMin = new Vector2(0.08f, 0.15f);
            rt.anchorMax = new Vector2(0.55f, 0.45f);
            rt.pivot = new Vector2(0.0f, 0.0f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else if (sceneName.Contains("Victory"))
        {
            // Victory: Chữ ở góc trên bên trái tránh đè Trái Đất (dưới-phải) và trạm không gian (giữa-trái)
            rt.anchorMin = new Vector2(0.08f, 0.62f);
            rt.anchorMax = new Vector2(0.55f, 0.88f);
            rt.pivot = new Vector2(0.0f, 1.0f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else if (sceneName.Contains("GameOver"))
        {
            // Game Over: Chữ ở nửa bên trái tránh đè xác tàu vũ trụ bị vỡ ở góc bên phải
            rt.anchorMin = new Vector2(0.08f, 0.35f);
            rt.anchorMax = new Vector2(0.45f, 0.75f);
            rt.pivot = new Vector2(0.0f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else
        {
            // Mặc định (MainMenu hoặc các scene khác): Giữa màn hình
            rt.anchorMin = new Vector2(0.12f, 0.32f);
            rt.anchorMax = new Vector2(0.88f, 0.68f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Center;
        }

        // Reset offsets để bám sát anchor mới
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ──────────────────────────────────────────────
    // TYPEWRITER EFFECT
    // ──────────────────────────────────────────────

    private IEnumerator TypeWriterEffect(string fullText)
    {
        // Chuyển toàn bộ text thành chữ in hoa do font Kenney Space hiển thị đẹp nhất ở dạng IN HOA
        string upperText = fullText.ToUpper();

        // Header nhỏ kiểu terminal
        const string HEADER = "<line-height=130%><cspace=0.25em><size=7><color=#A3E2F2>" +
                              "// ACCESSING SYSTEM LOG //</color></size></cspace>\n\n";
        
        // Body chữ trắng: Cân chỉnh cỡ chữ nhỏ gọn (size=8), khoảng cách chữ khít hơn (cspace=0.08em)
        // và khoảng cách dòng thông thoáng (line-height=150%) để đoạn văn dài hiển thị đẹp đẽ, cân đối.
        const string BODY_OPEN  = "<line-height=150%><cspace=0.08em><size=8><color=#FFFFFF>";
        const string BODY_CLOSE = "</color></size></cspace></line-height>";

        const float CHAR_DELAY = 0.025f;

        // Tag-aware typewriter loop to prevent raw Rich Text tags from flashing on the screen
        int charIndex = 0;
        System.Text.StringBuilder typedText = new System.Text.StringBuilder();
        
        while (charIndex < upperText.Length)
        {
            if (upperText[charIndex] == '<')
            {
                // Copy the entire tag at once (e.g. <COLOR=#FF3333> or </COLOR>)
                int tagCloseIndex = upperText.IndexOf('>', charIndex);
                if (tagCloseIndex != -1)
                {
                    typedText.Append(upperText.Substring(charIndex, tagCloseIndex - charIndex + 1));
                    charIndex = tagCloseIndex + 1;
                    continue; // Skip delay, process tags instantly
                }
            }
            
            typedText.Append(upperText[charIndex]);
            charIndex++;
            
            // Con trỏ nhấp nháy kiểu terminal
            string cursor = (charIndex < upperText.Length && charIndex % 2 == 0) ? "<color=#00FFFF>_</color>" : "";
            _narrativeText.text = HEADER + BODY_OPEN + typedText.ToString() + cursor + BODY_CLOSE;
            
            yield return new WaitForSecondsRealtime(CHAR_DELAY);
        }

        // Giữ nguyên 1.8s để người chơi đọc xong
        yield return new WaitForSecondsRealtime(1.8f);
    }

    // ──────────────────────────────────────────────
    // ASCII PROGRESS BAR
    // ──────────────────────────────────────────────

    private static string DrawProgressBar(float progress)
    {
        const int TOTAL = 20;
        int filled = Mathf.RoundToInt((progress / 0.9f) * TOTAL);
        System.Text.StringBuilder sb = new System.Text.StringBuilder("<color=#00FFFF>");
        for (int i = 0; i < TOTAL; i++)
        {
            if      (i <  filled) sb.Append('█');
            else if (i == filled) sb.Append('▒');
            else                  sb.Append("<color=#113333>░</color>");
        }
        sb.Append("</color>");
        return sb.ToString();
    }

    // ──────────────────────────────────────────────
    // SMOOTH FADE
    // ──────────────────────────────────────────────

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = _group.alpha, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / duration;
            float st = t * t * (3f - 2f * t); // SmoothStep
            _group.alpha = Mathf.Lerp(start, target, st);
            yield return null;
        }
        _group.alpha = target;
    }

    // ──────────────────────────────────────────────
    // SCENE → SPRITE / NARRATIVE MAPPING
    // ──────────────────────────────────────────────

    private Sprite GetBackground(string sceneName)
    {
        if (sceneName.Contains("Level1"))   return _bgLevel1;
        if (sceneName.Contains("Level2"))   return _bgLevel2;
        if (sceneName.Contains("Level3"))   return _bgLevel3;
        if (sceneName.Contains("Victory"))  return _bgVictory;
        if (sceneName.Contains("GameOver")) return _bgGameOver;
        return null;
    }

    private static string GetNarrative(string sceneName)
    {
        if (sceneName.Contains("Level1"))   return "Warning: <color=#FF3333>HOSTILE ALIEN ARMADA</color> detected entering <color=#00FFFF>SECTOR FOUR</color>. Outer planetary defense lines have collapsed. All pilots launch immediately. You are the <color=#33FF33>FINAL DEFENDER</color>.";
        if (sceneName.Contains("Level2"))   return "The alien swarm grows denser. Scanners detect <color=#FFBB00>ELITE HUNTER-CLASS STARFIGHTERS</color> intercepting our vector. <color=#FF6600>HEAVY ASTEROID</color> presence ahead. Steel your resolve, pilot.";
        if (sceneName.Contains("Level3"))   return "Critical alert: <color=#FF0055>MASSIVE BIO-MECHANICAL SIGNATURE</color> detected ahead. Energy readings surpass all known databases. Engage all weapon systems. Prepare for the <color=#FF0099>FINAL CONFRONTATION</color>.";
        if (sceneName.Contains("Victory"))  return "<color=#33FF33>MISSION ACCOMPLISHED</color>. The enemy fleet has been neutralized. Planetary systems are secured. The galaxy is <color=#00FFFF>SAFE ONCE MORE</color>... for now. Return to base for maintenance.";
        if (sceneName.Contains("GameOver")) return "<color=#FF3333>HULL INTEGRITY COMPROMISED</color>. Escape pod launch failed. System offline. The <color=#888888>DEFENDER HAS FALLEN</color>, and the stars slowly fade into absolute darkness...";
        if (sceneName.Contains("MainMenu")) return "Main systems online. <color=#00FFFF>QUANTUM LINK</color> established. Welcome back, Commander. Awaiting sector selection and combat orders.";
        return "Initiating <color=#FFBB00>HYPERSPACE JUMP</color>. Calculating warp vector. Establishing secure link to the adjacent combat zone...";
    }

    // ──────────────────────────────────────────────
    // CANVAS BUILDER (Runtime)
    // ──────────────────────────────────────────────

    private void BuildOverlayCanvas()
    {
        // Root canvas luôn nằm trên cùng tất cả UI khác
        GameObject root = new GameObject("[SceneTransitionOverlay]");
        root.transform.SetParent(transform, false);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99999;

        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        // Layer 1 ── Background (ảnh cốt truyện hoặc màu tối fallback)
        _bgImage = CreateImage(root, "StoryBackground", fallbackBgColor, stretch: true);

        // Layer 2 ── Vignette (rìa đen tối dần, sinh từ code)
        _vignetteSprite  = BuildVignetteSprite();
        _vignetteImage   = CreateImage(root, "VignetteOverlay", new Color(0f, 0f, 0f, 0.75f), stretch: true);
        _vignetteImage.sprite = _vignetteSprite;

        // Layer 3 ── Narrative text (Giảm fontSize mặc định ban đầu từ 16 xuống còn 6)
        _narrativeText = CreateTMP(root, "NarrativeText",
            anchorMin: new Vector2(0.12f, 0.32f),
            anchorMax: new Vector2(0.88f, 0.68f),
            fontSize: 6,
            color: Color.white,
            alignment: TextAlignmentOptions.Center,
            fontAsset: _narrativeFont);

        // Layer 4 ── Status / progress bar (Giảm fontSize mặc định ban đầu từ 11 xuống còn 4)
        _statusText = CreateTMP(root, "StatusText",
            anchorMin: new Vector2(0.04f, 0.04f),
            anchorMax: new Vector2(0.55f, 0.22f),
            fontSize: 8,
            color: new Color(accentCyberColor.r, accentCyberColor.g, accentCyberColor.b, 0.55f),
            alignment: TextAlignmentOptions.BottomLeft,
            fontAsset: _narrativeFont);
        _statusText.fontStyle = FontStyles.Bold;

        // Canvas Group để fade toàn bộ overlay cùng lúc
        _group = root.AddComponent<CanvasGroup>();
        _group.alpha          = 1f;
        _group.interactable   = false;
        _group.blocksRaycasts = false;

        // Ẩn UI cho đến khi cần dùng
        _narrativeText.gameObject.SetActive(false);
        _statusText.gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // CANVAS BUILDER UTILITIES
    // ──────────────────────────────────────────────

    private Image CreateImage(GameObject parent, string name, Color color, bool stretch)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        if (stretch) Stretch(go.GetComponent<RectTransform>());
        return img;
    }

    private TextMeshProUGUI CreateTMP(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        float fontSize, Color color, TextAlignmentOptions alignment,
        TMP_FontAsset fontAsset = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }
        tmp.text             = "";
        tmp.fontSize         = fontSize;
        tmp.color            = color;
        tmp.alignment        = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Tạo texture vignette gradient tròn bằng code (tránh phụ thuộc file asset bên ngoài)
    private static Sprite BuildVignetteSprite()
    {
        const int SIZE = 256;
        Texture2D tex    = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        Vector2   center = new Vector2(SIZE / 2f, SIZE / 2f);
        float     maxD   = SIZE / 2f;

        for (int y = 0; y < SIZE; y++)
        for (int x = 0; x < SIZE; x++)
        {
            float dist  = Vector2.Distance(new Vector2(x, y), center);
            float alpha = Mathf.Clamp01(dist / maxD);
            alpha       = alpha * alpha; // Bình phương: tâm trong suốt, rìa tối gắt
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, SIZE, SIZE), new Vector2(0.5f, 0.5f));
    }
}