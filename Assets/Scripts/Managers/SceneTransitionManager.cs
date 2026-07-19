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
/// - Phát âm thanh radio "tít tít" cơ học đồng bộ với từng ký tự xuất hiện
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    // INSPECTOR SETTINGS
    // ──────────────────────────────────────────────

    [Header("Transition Timing")]
    [Tooltip("Thời gian tối dần để che đi scene cũ (Tạo độ mượt mà đậm tính điện ảnh)")]
    public float fadeOutDuration = 0.8f;
    [Tooltip("Thời gian sáng dần để hiện ra scene mới")]
    public float fadeInDuration  = 1.0f;

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

    [Header("End Credit")]
    [Tooltip("(Tuỳ chọn) Ảnh nền riêng cho End Credit — để trống sẽ dùng bg_story_victory")]
    [SerializeField] private Sprite _bgEndCredit;
    [Tooltip("AudioManager bgmClips key cho nhạc end credit (mặc định: bgm_endcredit)")]
    [SerializeField] private string _endCreditBgmKey = "bgm_endcredit";

    [Header("Font")]
    [Tooltip("Drag: Assets/Fonts/Kenny_Space/Kenney Space SDF")]
    [SerializeField] private TMP_FontAsset _narrativeFont;

    [Header("Sci-Fi Radio Audio")]
    [Tooltip("Nếu để trống, hệ thống sẽ tự động tạo âm thanh bíp bíp radio bằng code")]
    [SerializeField] private AudioClip _beepSound;
    [Range(0f, 1f)]
    [SerializeField] private float _beepVolume = 0.12f;
    [Range(0.5f, 2f)]
    [SerializeField] private float _beepPitchMin = 0.93f;
    [Range(0.5f, 2f)]
    [SerializeField] private float _beepPitchMax = 1.07f;

    // ──────────────────────────────────────────────
    // PRIVATE RUNTIME REFERENCES
    // ──────────────────────────────────────────────

    private Canvas             _canvas;
    private CanvasGroup        _group;
    private Image              _bgImage;       // Ảnh nền cốt truyện
    private Image              _vignetteImage; // Lớp tối rìa màn hình
    private TextMeshProUGUI    _narrativeText; // Chữ cốt truyện typewriter
    private TextMeshProUGUI    _statusText;    // Progress bar / trạng thái hệ thống
    private Sprite             _vignetteSprite;
    private AudioSource        _audioSource;

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
        ConfigureAudioSource();
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

    /// <summary>Tự động thiết lập AudioSource tại Runtime.</summary>
    private void ConfigureAudioSource()
    {
        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // Âm thanh 2D (phát trực tiếp vào tai người chơi)

        // Nếu không có âm thanh kéo vào, tự tạo tiếng bíp ngắn dạng sóng hình sin
        if (_beepSound == null)
        {
            _beepSound = CreateProceduralBeep();
        }
    }

    private void Start()
    {
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

    /// <summary>
    /// Phát End Credit cinematic (~73 giây) đồng bộ với endcredit_music,
    /// sau đó tự động về MainMenu. Gọi từ VictoryController.
    /// </summary>
    public void PlayEndCredits()
    {
        if (_isTransitioning) return;
        StartCoroutine(EndCreditSequence());
    }

    // ──────────────────────────────────────────────
    // END CREDIT PIPELINE  (73 giây — đồng bộ nhạc)
    // ──────────────────────────────────────────────

    private IEnumerator EndCreditSequence()
    {
        _isTransitioning = true;
        float startTime = Time.unscaledTime; // Lưu mốc thời gian bắt đầu phát nhạc

        // ── 1. TẮT TẤT CẢ ÂM THANH, BẮT ĐẦU NHẠC END CREDIT ─────────
        AudioManager.Instance?.StopAllLevelSounds();
        
        float bgmVolume = AudioManager.Instance != null ? AudioManager.Instance.bgmVolume : 1f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGMOnce(_endCreditBgmKey);
            AudioManager.Instance.SetBGMVolume(0f);
            StartCoroutine(FadeAudioManagerBGM(0f, bgmVolume, 1.5f));
        }

        // ── 2. HIỆN LỚP PHỦ + NỀN ─────────────────────────────────────
        Sprite bgSprite = _bgEndCredit != null ? _bgEndCredit : _bgVictory;
        HideSceneCanvases();
        ApplyBackground(bgSprite);

        if (_narrativeText != null)
        {
            RectTransform rt = _narrativeText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.08f, 0.52f);
                rt.anchorMax = new Vector2(0.92f, 0.88f);
                rt.pivot     = new Vector2(0.5f, 1.0f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            _narrativeText.alignment = TextAlignmentOptions.Center;
        }

        ShowUI(true);
        yield return StartCoroutine(FadeTo(1f, 1.2f));   // Fade in overlay nhanh

        // ── 3. TYPEWRITER EPILOGUE (Gõ chữ có hỗ trợ skip) ────────────
        string epilogue =
            "<color=#00FFFF>A.E.G.I.S [RECONSTRUCTED MISSION LOG — SYSTEM TIME: +08:12]:</color>\n\n" +
            "[SILENCE]" +
            "Singularity collapse confirmed. Reactor core temperatures: stabilized.[BEAT]\n" +
            "Tactical sensor array shows no remaining hostile bio-signatures within this sector.[BEAT]\n" +
            "The long night has ended. We have crossed the threshold, and we have survived.[BEAT][BEAT]\n\n" +
            "[SILENCE]" +
            "<color=#FFBB00>CHIEF ENG SARAH [COMMS RECORDING]:</color>\n" +
            "\"Sarah here... to anyone left out there... the atmospheric shields are holding.[BEAT]\n" +
            "The skies are clear again. The orbital station repairs are already underway.[BEAT]\n" +
            "We did it. We actually made it home...\"[BEAT][BEAT]\n\n" +
            "[SILENCE]" +
            "<color=#A3E2F2>COMMANDER VANCE [FINAL TRANSMISSION — ENCRYPTED]:</color>\n" +
            "\"I don't know how to write a eulogy for someone who is still breathing.[BEAT] So I won't.[BEAT]\n" +
            "I'll just say this. The galaxy was going dark.[BEAT] You flew straight into the abyss\n" +
            "and you brought the light back.[BEAT] Rest now, Pilot. Aegis. You've earned it.\"\n\n" +
            "[SILENCE]" +
            "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT — REGISTRY LOG]:</color>\n" +
            "\"Three months after contact. Earth's oceans are visible once more through the clearing dust.[BEAT]\n" +
            "The prototype fighter has been retired to the central museum as a monument to hope.[BEAT]\n" +
            "But the stars remain silent, watching the peace you fought so hard to defend.[BEAT][BEAT]\n\n" +
            "[SILENCE]" +
            "System status: Nominal.[BEAT] Thank you for being my pilot.[BEAT] Signing off.\"";

        yield return StartCoroutine(TypeWriterEffect(epilogue));

        // ── 4. KHOẢNG LẶNG ĐỌC CỐT TRUYỆN (Đến giây thứ 52 hoặc click để bỏ qua) ──
        float elapsed = Time.unscaledTime - startTime;
        float readTimeout = 52.0f - elapsed;
        float readTimer = 0f;
        while (readTimer < readTimeout)
        {
            // Nếu người chơi click chuột hoặc bấm Space/Enter, bỏ qua luôn phần chờ đọc
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                break;
            }
            readTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // ── 5. FADE TO COMPLETE BLACK (Tắt UI cốt truyện, chuyển sang nền đen) ──
        ShowUI(false);
        if (_bgImage != null)
        {
            _bgImage.sprite = null;
            _bgImage.color  = Color.black;
        }
        if (_vignetteImage != null) _vignetteImage.color = new Color(0f, 0f, 0f, 0f);
        yield return StartCoroutine(FadeTo(1f, 0.5f)); 

        // Chờ trên màn hình đen (Đến giây thứ 58 hoặc click để bỏ qua)
        float elapsed2 = Time.unscaledTime - startTime;
        float blackTimeout = 58.0f - elapsed2;
        float blackTimer = 0f;
        while (blackTimer < blackTimeout)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                break;
            }
            blackTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // ── 6. "THE END" XUẤT HIỆN + PULSE (Chính xác 15 giây tổng cộng hoặc click để qua) ──
        TextMeshProUGUI theEndLabel = CreateTMP(
            _canvas.gameObject,
            "TheEndLabel",
            anchorMin:  new Vector2(0.1f, 0.35f),
            anchorMax:  new Vector2(0.9f, 0.65f),
            fontSize:   48f,
            color:      new Color(0f, 1f, 0.9f, 0f),
            alignment:  TextAlignmentOptions.Center,
            fontAsset:  _narrativeFont);
        theEndLabel.fontStyle = FontStyles.Bold;
        theEndLabel.text = "THE END";

        // Fade in THE END (2 giây)
        yield return StartCoroutine(FadeTMPAlpha(theEndLabel, 0f, 1f, 2.0f));

        // Pulse + giữ trong 10 giây (hoặc click để kết thúc sớm)
        float holdTimer = 0f;
        float holdDuration = 10.0f;
        while (holdTimer < holdDuration)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                break;
            }
            holdTimer += Time.unscaledDeltaTime;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 1.6f);
            theEndLabel.color = new Color(pulse * 0.0f, pulse * 1.0f, pulse * 0.9f + 0.1f, 1f);
            float s = 1f + 0.018f * Mathf.Sin(Time.unscaledTime * 1.1f);
            RectTransform lrt = theEndLabel.GetComponent<RectTransform>();
            if (lrt != null) lrt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // ── 7. FADE OUT NHẠC + MÀN HÌNH, VỀ MAIN MENU (~3.0 giây) ──────
        AudioManager.Instance?.FadeBGMOut(bgmVolume, 0f, 3.0f);
        yield return StartCoroutine(FadeTMPAlpha(theEndLabel, 1f, 0f, 3.0f));
        if (theEndLabel != null) Destroy(theEndLabel.gameObject);

        if (_vignetteImage != null)
            _vignetteImage.color = new Color(0f, 0f, 0f, 0.75f);

        yield return new WaitForSecondsRealtime(0.5f);

        _isTransitioning = false;
        LoadScene("MainMenu");
    }

    // ── Fade volume BGM qua AudioManager (dùng SetBGMVolume từng frame) ─
    private IEnumerator FadeAudioManagerBGM(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float st = t * t * (3f - 2f * t);
            AudioManager.Instance?.SetBGMVolume(Mathf.Lerp(from, to, st));
            yield return null;
        }
        AudioManager.Instance?.SetBGMVolume(to);
    }

    // ── Fade volume của một AudioSource riêng ──────────────────────────
    private IEnumerator FadeAudioSource(AudioSource src, float from, float to, float duration)
    {
        if (src == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float st = t * t * (3f - 2f * t); // SmoothStep
            src.volume = Mathf.Lerp(from, to, st);
            yield return null;
        }
        src.volume = to;
    }

    // ── Fade alpha của một TextMeshProUGUI ─────────────────────────────
    private IEnumerator FadeTMPAlpha(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        if (tmp == null) yield break;
        float elapsed = 0f;
        Color c = tmp.color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float st = t * t * (3f - 2f * t);
            tmp.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, st));
            yield return null;
        }
        tmp.color = new Color(c.r, c.g, c.b, to);
    }

    // ──────────────────────────────────────────────
    // TRANSITION PIPELINE
    // ──────────────────────────────────────────────

    private IEnumerator Transition(string sceneName, System.Func<AsyncOperation> loadAction)
    {
        _isTransitioning = true;

        string narrative = GetNarrative(sceneName);
        Sprite bgSprite  = GetBackground(sceneName);

        HideSceneCanvases();
        ApplyBackground(bgSprite);
        ConfigureTextLayout(sceneName);

        ShowUI(true);
        yield return StartCoroutine(FadeTo(1f, fadeOutDuration));

        yield return StartCoroutine(TypeWriterEffect(narrative));

        AsyncOperation op = loadAction.Invoke();
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            SetStatus($"DOWNLOADING DATA REPOSITORIES [{Mathf.RoundToInt(op.progress * 100f)}%]\n" +
                      DrawProgressBar(op.progress));
            yield return null;
        }

        SetStatus("SYNCHRONIZATION COMPLETE. READY TO EMERGE.");
        yield return new WaitForSecondsRealtime(1.2f); 

        op.allowSceneActivation = true;
        yield return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isTransitioning)
        {
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

    private void HideSceneCanvases()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>())
        {
            if (c == _canvas || (c.transform.parent != null && c.transform.IsChildOf(transform)))
                continue;
            c.enabled = false;
        }
    }

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
            rt.anchorMin = new Vector2(0.12f, 0.70f);
            rt.anchorMax = new Vector2(0.88f, 0.92f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Center;
        }
        else if (sceneName.Contains("Level2"))
        {
            rt.anchorMin = new Vector2(0.08f, 0.25f);
            rt.anchorMax = new Vector2(0.45f, 0.75f);
            rt.pivot = new Vector2(0.0f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else if (sceneName.Contains("Level3"))
        {
            rt.anchorMin = new Vector2(0.08f, 0.15f);
            rt.anchorMax = new Vector2(0.55f, 0.45f);
            rt.pivot = new Vector2(0.0f, 0.0f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else if (sceneName.Contains("Victory"))
        {
            rt.anchorMin = new Vector2(0.08f, 0.62f);
            rt.anchorMax = new Vector2(0.55f, 0.88f);
            rt.pivot = new Vector2(0.0f, 1.0f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else if (sceneName.Contains("GameOver"))
        {
            rt.anchorMin = new Vector2(0.08f, 0.35f);
            rt.anchorMax = new Vector2(0.45f, 0.75f);
            rt.pivot = new Vector2(0.0f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Left;
        }
        else
        {
            rt.anchorMin = new Vector2(0.12f, 0.32f);
            rt.anchorMax = new Vector2(0.88f, 0.68f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _narrativeText.alignment = TextAlignmentOptions.Center;
        }

        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ──────────────────────────────────────────────
    // TYPEWRITER EFFECT WITH AUDIO
    // ──────────────────────────────────────────────

    private IEnumerator TypeWriterEffect(string fullText)
    {
        string upperText = fullText.ToUpper();

        const string HEADER = "<line-height=130%><cspace=0.25em><size=7><color=#A3E2F2>" +
                             "// MULTI-CHANNEL TACTICAL FEED OPEN //</color></size></cspace>\n\n";
        
        const string BODY_OPEN  = "<line-height=150%><cspace=0.08em><size=8><color=#FFFFFF>";
        const string BODY_CLOSE = "</color></size></cspace></line-height>";

        // Tốc độ mặc định ban đầu
        float currentDelay = 0.018f; 

        int charIndex = 0;
        System.Text.StringBuilder typedText = new System.Text.StringBuilder();
        bool isSkipped = false;

        // Hàm helper cục bộ để làm sạch chuỗi
        string CleanMarkupForDisplay(string text)
        {
            return text.Replace("[BEAT]", "").Replace("[SILENCE]", "");
        }

        while (charIndex < upperText.Length)
        {
            // Kiểm tra click chuột trái hoặc bấm phím Space/Enter để skip nhanh
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                isSkipped = true;
                break;
            }

            // 1. XỬ LÝ CÁC THẺ RICH TEXT CỦA UNITY (Bỏ qua không gõ và không tính delay)
            if (upperText[charIndex] == '<')
            {
                int tagCloseIndex = upperText.IndexOf('>', charIndex);
                if (tagCloseIndex != -1)
                {
                    typedText.Append(upperText.Substring(charIndex, tagCloseIndex - charIndex + 1));
                    charIndex = tagCloseIndex + 1;
                    continue; 
                }
            }

            // 2. PHÂN TÍCH CẢM XÚC & ĐIỀU CHỈNH TỐC ĐỘ GÕ THEO BỐI CẢNH (Emotion Dynamics)
            string textAnalyze = upperText.Substring(0, Mathf.Min(charIndex + 20, upperText.Length));
            
            if (textAnalyze.Contains("VANCE [WEAK") || textAnalyze.Contains("FALLING COMM"))
            {
                currentDelay = 0.06f; // Vance kiệt sức -> gõ chậm
            }
            else if (textAnalyze.Contains("[REPEATED") || textAnalyze.Contains("SCREAM") || textAnalyze.Contains("DANGER"))
            {
                currentDelay = 0.01f; // Hoảng loạn -> gõ cực nhanh
            }
            else if (textAnalyze.Contains("HARBINGER FREQUENCY"))
            {
                currentDelay = 0.035f; // Giọng boss -> chậm ma mị
            }
            else
            {
                currentDelay = 0.018f;
            }

            // 3. XỬ LÝ NHỊP NGHỈ ĐIỆN ẢNH (Dramatic Pauses)
            if (upperText.Substring(charIndex).StartsWith("[BEAT]"))
            {
                charIndex += 6; // Bỏ qua "[BEAT]"
                float pauseTimer = 0f;
                while (pauseTimer < 0.6f)
                {
                    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        isSkipped = true;
                        break;
                    }
                    pauseTimer += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (isSkipped) break;
                continue;
            }
            
            if (upperText.Substring(charIndex).StartsWith("[SILENCE]"))
            {
                charIndex += 9; // Bỏ qua "[SILENCE]"
                float pauseTimer = 0f;
                while (pauseTimer < 1.2f)
                {
                    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        isSkipped = true;
                        break;
                    }
                    pauseTimer += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (isSkipped) break;
                continue;
            }

            // 4. TIẾN HÀNH IN CHỮ VÀ PHÁT ÂM THANH
            char currentChar = upperText[charIndex];
            typedText.Append(currentChar);
            charIndex++;
            
            if (currentChar != ' ' && currentChar != '\n' && _audioSource != null && _beepSound != null)
            {
                float speedFactor = 0.018f / currentDelay;
                _audioSource.pitch = Random.Range(_beepPitchMin, _beepPitchMax) * Mathf.Clamp(speedFactor, 0.8f, 1.3f);
                _audioSource.PlayOneShot(_beepSound, _beepVolume);
            }

            string cursor = (charIndex < upperText.Length && charIndex % 2 == 0) ? "<color=#00FFFF>_</color>" : "";
            _narrativeText.text = HEADER + BODY_OPEN + typedText.ToString() + cursor + BODY_CLOSE;
            
            // Đợi từng ký tự nhưng vẫn lắng nghe lệnh skip từng frame
            float elapsed = 0f;
            while (elapsed < currentDelay)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    isSkipped = true;
                    break;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (isSkipped) break;
        }

        if (isSkipped)
        {
            // Hiển thị toàn bộ text sạch
            string cleanText = CleanMarkupForDisplay(upperText);
            _narrativeText.text = HEADER + BODY_OPEN + cleanText + BODY_CLOSE;
            if (_audioSource != null && _beepSound != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.PlayOneShot(_beepSound, _beepVolume * 1.5f);
            }
        }

        // Chờ thêm một nhịp cuối trước khi tiếp tục
        float endHold = isSkipped ? 1.0f : 3.0f;
        float holdTimer = 0f;
        while (holdTimer < endHold)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                break;
            }
            holdTimer += Time.unscaledDeltaTime;
            yield return null;
        }
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
    // SMOOTH FADE (SmoothStep)
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
    // SCENE → SPRITE / NARRATIVE MAPPING (Cốt truyện siêu dài, đa nhân vật)
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
        if (sceneName.Contains("Level1"))   
        {
            return "<color=#A3E2F2>COMMANDER VANCE [COMMS]:</color> \"Damn it! Someone tell me why Aegis-7 is not responding!\" " +
                   "\"Vanguard-1? Vanguard-2? ... *STATIC* ... Oh god, they are gone. The whole vanguard squadron... gone in seconds.\"\n\n" +
                   "<color=#FFBB00>CHIEF ENG SARAH [ENG]:</color> \"Vance! Stop staring at the casualty screen! " +
                   "The alien orbital beam just sliced the lower drydocks in half! " +
                   "The launch bays are warping under 3000-degree plasma heat. If we don't drop this prototype right now, " +
                   "the pilot will be crushed under 50,000 tons of structural steel!\"\n\n" +
                   "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Analyzing neural synchronization... 100% stable. [BEAT] " +
                   "Commander Vance, Chief Sarah—stop arguing. " +
                   "I have initiated automated emergency ignition. Bypassing drydock structural locks. Bypassing fuel pressure safeties.\"\n\n" +
                   "<color=#FFBB00>SARAH [ENG]:</color> \"Wait! The cooling core isn't fully pressurized! If they launch now, the engines might—\"\n\n" +
                   "<color=#A3E2F2>VANCE [COMMS]:</color> \"There is no 'later' Sarah! [BEAT] ... Pilot. If you can hear my voice... " +
                   "you are the last sword humanity has left. " +
                   "Go show these bastards why we don't go quietly into the dark. <color=#33FF33>IGNITE THRUSTERS AND RUN!</color>\"";
        }
        
        if (sceneName.Contains("Level2"))   
        {
            return "<color=#FFBB00>CHIEF ENG SARAH [ENG]:</color> \"*COUGHING*... *HEAVY BREATHING*... Comms... do you copy? " +
                   "Vance is... he was caught in the command deck collapse. I'm patching through from a burning backup terminal in Sector-C.\"\n\n" +
                   "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Engaging primary coolant pumps. " +
                   "The orbital breakout was successful, Sarah. However, long-range radar indicates a critical blockade ahead.\"\n\n" +
                   "<color=#888888>DISTANT COMLINK [RADIO INTERFERENCE]:</color> \"*STATIC*... This is Specter-3! They're in the rocks! " +
                   "The asteroids are... *SCREAM*... they are shifting! NO!— *LOUD EXPLOSION* ... *STATIC*\"\n\n" +
                   "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Specter-3 signal lost. [SILENCE] " +
                   "A fleet of <color=#FF6600>Spectre-Class Interceptors</color> has locked down the <color=#FFBB00>Acheron Asteroid Belt</color>.\"\n\n" +
                   "<color=#A3E2F2>VANCE [WEAK/GROANING]:</color> \"*GROANS*... Pilot... listen to me. Don't let their deaths... be for nothing. " +
                   "Trust the AI's steering calculations. The belt's gravitational field is completely erratic. " +
                   "One micro-second error... and the debris will liquefy your cockpit. " +
                   "Sarah... upload the <color=#00FFFF>Hyperion Overcharge</color> codes. [BEAT] Pilot... fly like you want to live.\"";
        }
        
        if (sceneName.Contains("Level3"))   
        {
            return "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Warning. Spatial distortions detected. " +
                   "We have entered the gravity well of the central singularity. " +
                   "The Harbinger is... [BEAT] ... far larger than our archives predicted. It has already anchored itself to Earth's mantle.\"\n\n" +
                   "<color=#FF0055>UNKNOWN TRANSLATION [HARBINGER FREQUENCY]:</color> \"*DISSONANT MECHANICAL VOICES*... " +
                   "YOUR SPECIES HAS CHOSEN RESISTANCE. RESISTANCE IS AN ERROR. [BEAT] " +
                   "CARBON RESIDUE WILL BE RESTRUCTURED. ASSIMILATION SEQUENCE: INITIATED.\"\n\n" +
                   "<color=#FFBB00>CHIEF ENG SARAH [ENG]:</color> \"The whole station is breaking apart! [BEAT] Pilot! " +
                   "The Harbinger is draining the thermal energy directly from our planet's molten core to shield itself! " +
                   "The tectonic plates are cracking... we have only minutes before Earth implodes!\"\n\n" +
                   "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Our current weapon payload cannot penetrate their thermal shield. " +
                   "To break through, we must overload the engine block. The heat will surpass <color=#FF3333>1500°C</color>... " +
                   "causing a total core meltdown. [SILENCE]\"\n\n" +
                   "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Analyzing pilot's heart rate... 140 BPM. " +
                   "Analyzing neural patterns... I see. You have no intention of retreating. " +
                   "Very well. Disabling all safety protocols. Disabling thermal limiters. " +
                   "It has been an honor, Pilot. <color=#FF3333>PREPARE FOR RAMMING SPEED!</color>\"";
        }
        
        if (sceneName.Contains("Victory"))  
        {
            return "<color=#A3E2F2>VANCE [STATIC-HEAVY COMMS]:</color> \"*LAUGHING*... *COUGHING BLOOD*... Pilot?! Pilot, do you copy me?! " +
                   "By god... you actually did it! The Harbinger's core is collapsing! Look at their fleet!\"\n\n" +
                   "<color=#FFBB00>CHIEF ENG SARAH [ENG]:</color> \"Oh my god... *SOBBING IN RELIEF*... " +
                   "I'm seeing planetary tectonic readings stabilizing! The core is holding! " +
                   "Look at the sky... those aren't warheads, those are the pieces of their fleet burning like shooting stars!\"\n\n" +
                   "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Scanning local airspace. All enemy bio-mechanical signatures have ceased. " +
                   "System integrity is at 8%. Major hull breaches detected. [BEAT] " +
                   "But we are functional. We are... home.\"\n\n" +
                   "<color=#A3E2F2>VANCE [COMMS]:</color> \"You saved us, kid. You brought <color=#00FFFF>LIGHT AND LIFE</color> back to this freezing galaxy. " +
                   "Sarah, get the repair drones ready. We are bringing our hero back to the hangar.\"";
        }
        
        if (sceneName.Contains("GameOver")) 
        {
            return "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Critical failure. Engine core melted. " +
                   "Life support systems... offline. " +
                   "Pilot... I am trying to bypass the secondary ejection gears... " +
                   "but the cockpit frame has warped from the pressure. [BEAT] I cannot open the hatch.\"\n\n" +
                   "<color=#FFBB00>CHIEF ENG SARAH [REPEATED DISTANT COMMS]:</color> \"Pilot! Eject! Do you copy?! " +
                   "Please, just pull the manual lever! VANCE! HELP ME! THE SIGNAL IS FADING!\"\n\n" +
                   "<color=#A3E2F2>VANCE [FALLING COMM SIGNAL]:</color> \"Pilot... stay with me! " +
                   "Don't you dare close your eyes! Vance to Vanguard! Do you read me?! ... *STATIC* ... Please...\"\n\n" +
                   "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Oxygen reserves... 1%. " +
                   "The stars... they look remarkably quiet today, don't they? " +
                   "Thank you for piloting with me. Initiating... final... shutdown... " +
                   "<color=#888888>Goodbye, my friend.</color>\"";
        }
        
        if (sceneName.Contains("MainMenu")) 
        {
            return "<color=#00FFFF>A.E.G.I.S BOOT PROTOCOL v4.02:</color> \"Initializing quantum cores...\"\n" +
                   "<color=#FFBB00>SYSTEM LOG:</color> \"Drydock repairs: 94% complete. Core shield generator: ONLINE.\"\n\n" +
                   "<color=#A3E2F2>VANCE [ARCHIVED VOICE MESSAGE]:</color> \"Commander, if you are reading this, the network link is stable. " +
                   "We have patched the sensory grid, but the long-range scout drones are already reporting " +
                   "bizarre gravity fluctuations near the outer rim. [BEAT] Something massive is moving in the shadows.\"\n\n" +
                   "<color=#FFBB00>CHIEF ENG SARAH [SYSTEM REPORT]:</color> \"Don't worry about the prototype ship—I've double-reinforced " +
                   "the engine manifolds. She's ready to fly whenever you are, Commander. Just give us the word.\"";
        }
        
        return "<color=#00FFFF>A.E.G.I.S [AI CO-PILOT]:</color> \"Plotting course through the Einstein-Rosen wormhole. " +
               "Quantum friction is dangerously high, pilot. Hang on tight to the manual restraints!\"\n\n" +
               "<color=#FFBB00>CHIEF ENG SARAH [ENG]:</color> \"I'm boosting the ship's magnetic field to <color=#00FFFF>120%</color> " +
               "to keep the radiation from tearing you apart! See you on the other side of the leap!\"\n\n" +
               "<color=#A3E2F2>VANCE [COMMS]:</color> \"Drop-out coordinates synchronized with the tactical satellites. " +
               "Prepare for space-time deceleration... three... two... one... JUMP!\"";
    }

    // ──────────────────────────────────────────────
    // CANVAS BUILDER (Runtime)
    // ──────────────────────────────────────────────

    private void BuildOverlayCanvas()
    {
        GameObject root = new GameObject("[SceneTransitionOverlay]");
        root.transform.SetParent(transform, false);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99999;

        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        _bgImage = CreateImage(root, "StoryBackground", fallbackBgColor, stretch: true);

        _vignetteSprite  = BuildVignetteSprite();
        _vignetteImage   = CreateImage(root, "VignetteOverlay", new Color(0f, 0f, 0f, 0.75f), stretch: true);
        _vignetteImage.sprite = _vignetteSprite;

        _narrativeText = CreateTMP(root, "NarrativeText",
            anchorMin: new Vector2(0.12f, 0.32f),
            anchorMax: new Vector2(0.88f, 0.68f),
            fontSize: 6,
            color: Color.white,
            alignment: TextAlignmentOptions.Center,
            fontAsset: _narrativeFont);

        _statusText = CreateTMP(root, "StatusText",
            anchorMin: new Vector2(0.04f, 0.04f),
            anchorMax: new Vector2(0.55f, 0.22f),
            fontSize: 8,
            color: new Color(accentCyberColor.r, accentCyberColor.g, accentCyberColor.b, 0.55f),
            alignment: TextAlignmentOptions.BottomLeft,
            fontAsset: _narrativeFont);
        _statusText.fontStyle = FontStyles.Bold;

        _group = root.AddComponent<CanvasGroup>();
        _group.alpha          = 1f;
        _group.interactable   = false;
        _group.blocksRaycasts = false;

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
            alpha       = alpha * alpha;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, SIZE, SIZE), new Vector2(0.5f, 0.5f));
    }

    // ──────────────────────────────────────────────
    // PROCEDURAL AUDIO GENERATOR
    // ──────────────────────────────────────────────

    private static AudioClip CreateProceduralBeep()
    {
        int sampleRate = 44100;
        float duration = 0.04f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float frequency = 1200f; 

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sine = Mathf.Sin(2f * Mathf.PI * frequency * t);
            float envelope = 1f - ((float)i / sampleCount); 
            samples[i] = sine * envelope;
        }

        AudioClip clip = AudioClip.Create("ProceduralBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}