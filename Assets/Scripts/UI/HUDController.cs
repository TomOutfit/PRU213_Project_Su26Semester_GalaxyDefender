using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls the gameplay Heads-Up Display (HUD).
/// Manages HP/Shield sliders, score, wave, and lives displays using TextMesh Pro.
/// Dynamically attaches visual effects and handles message displays.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Sliders")]
    public Slider HPSlider;
    public Slider ShieldSlider;

    [Header("TextMesh Pro Elements")]
    public TMP_Text ScoreTextTMP;
    public TMP_Text WaveTextTMP;
    public TMP_Text LivesTextTMP;

    [Header("Message Settings")]
    public TMP_FontAsset messageFont;

    private HUDVfx _vfx;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        // ── Khởi tạo _vfx trước để AnimateLives/AnimateScore gọi được ngay từ đầu ──
        _vfx = GetComponent<HUDVfx>();
        if (_vfx == null)
            _vfx = gameObject.AddComponent<HUDVfx>();

        // Auto-resolve text references trước khi subscribe events
        if (ScoreTextTMP == null) ScoreTextTMP = transform.Find("ScorePanel/ScoreText")?.GetComponent<TMP_Text>();
        if (WaveTextTMP == null)  WaveTextTMP  = transform.Find("WavePanel/WaveText")?.GetComponent<TMP_Text>();
        if (LivesTextTMP == null) LivesTextTMP  = transform.Find("LivesPanel/LivesText")?.GetComponent<TMP_Text>();

        // Feed references into VFX ngay
        _vfx.HPSlider    = HPSlider;
        _vfx.ShieldSlider = ShieldSlider;
        if (ScoreTextTMP != null) _vfx.scoreTextRect = ScoreTextTMP.GetComponent<RectTransform>();
        if (WaveTextTMP  != null) _vfx.waveTextRect  = WaveTextTMP.GetComponent<RectTransform>();
        if (LivesTextTMP != null) _vfx.livesTextRect  = LivesTextTMP.GetComponent<RectTransform>();

        // Auto-resolve components if not set in Inspector
        if (HPSlider == null) HPSlider = transform.Find("HPBar")?.GetComponent<Slider>();
        if (ShieldSlider == null) ShieldSlider = transform.Find("ShieldBar")?.GetComponent<Slider>();

        // Set max values to 100% unconditionally
        if (HPSlider != null) HPSlider.maxValue = 100f;
        if (ShieldSlider != null) ShieldSlider.maxValue = 100f;

        // Subscribe to PlayerHealth events
        playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHPChanged.AddListener(UpdateHP);
            playerHealth.OnShieldChanged.AddListener(UpdateShield);

            // Set initial values
            UpdateHP(playerHealth.currentHP);
            UpdateShield(playerHealth.currentShield);
        }

        // Subscribe to ScoreManager events
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.AddListener(UpdateScore);
            UpdateScore(ScoreManager.Instance.currentScore);
        }

        // Subscribe to WaveManager events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged.AddListener(UpdateWave);
            WaveManager.Instance.OnEnemyKilled.AddListener(UpdateEnemyCount);
            // Default to WAVE 1 until first wave actually starts spawning
            UpdateWave(WaveManager.Instance.waves != null ? 1 : 0);
        }

        // Subscribe to GameManager lives
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged.AddListener(UpdateLives);
            UpdateLives(GameManager.Instance.GetCurrentLives());
        }
    }

    private void Start()
    {
        // Fallback resolve nếu chưa có từ Awake (ví dụ khi assign qua Inspector không đầy đủ)
        if (ScoreTextTMP == null) ScoreTextTMP = transform.Find("ScorePanel/ScoreText")?.GetComponent<TMP_Text>();
        if (WaveTextTMP  == null) WaveTextTMP  = transform.Find("WavePanel/WaveText")?.GetComponent<TMP_Text>();
        if (LivesTextTMP == null) LivesTextTMP  = transform.Find("LivesPanel/LivesText")?.GetComponent<TMP_Text>();

        // Apply visual bar frame sprites dynamically for a polished look
        EnsureHUDSprites(HPSlider, "Assets/Sprites/UI/ui_healthbar_fill.png");
        EnsureHUDSprites(ShieldSlider, "Assets/Sprites/UI/ui_shieldbar_fill.png");

        // Đảm bảo references mới nhất được cập nhật vào VFX (trước đó Awake đã làm phần này)
        if (ScoreTextTMP != null) _vfx.scoreTextRect = ScoreTextTMP.GetComponent<RectTransform>();
        if (WaveTextTMP  != null) _vfx.waveTextRect  = WaveTextTMP.GetComponent<RectTransform>();
        if (LivesTextTMP != null) _vfx.livesTextRect  = LivesTextTMP.GetComponent<RectTransform>();

        // Kick-off count-up animation cho Lives ngay khi scene bắt đầu
        if (GameManager.Instance != null)
            UpdateLives(GameManager.Instance.GetCurrentLives());

        // Fallback for PlayerHealth in case it was null during Awake
        if (playerHealth == null)
        {
            playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnHPChanged.RemoveListener(UpdateHP);
                playerHealth.OnShieldChanged.RemoveListener(UpdateShield);
                playerHealth.OnHPChanged.AddListener(UpdateHP);
                playerHealth.OnShieldChanged.AddListener(UpdateShield);
                UpdateHP(playerHealth.currentHP);
                UpdateShield(playerHealth.currentShield);
            }
        }

        // Hiển thị hướng dẫn điều khiển khi bắt đầu Level 1
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level1")
        {
            ShowTutorialOverlay();
        }
    }

    private void ShowTutorialOverlay()
    {
        // Tạo Panel chính chứa hướng dẫn điều khiển
        GameObject tutorialPanel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(CanvasGroup));
        tutorialPanel.transform.SetParent(transform, false);

        RectTransform panelRect = tutorialPanel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(460f, 280f);

        // Tạo background tối bán trong suốt (xanh đen vũ trụ đậm)
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.04f, 0.04f, 0.08f, 0.94f);
        
        Sprite borderSprite = LoadSpriteRuntime("Assets/Sprites/UI/ui_bar_bg.png");
        if (borderSprite != null)
        {
            bgImg.sprite = borderSprite;
            bgImg.type = Image.Type.Sliced;
        }

        // Tạo dải màu trang trí phía trên (neon cyan)
        GameObject headerObj = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -20f);
        headerRect.sizeDelta = new Vector2(-40f, 4f);
        Image headerImg = headerObj.GetComponent<Image>();
        headerImg.color = new Color(0f, 0.9f, 1f, 0.6f);

        // Tạo tiêu đề CONTROLS
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 95f);
        titleRect.sizeDelta = new Vector2(420f, 40f);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "MISSION BRIEFING";
        titleText.fontSize = 24;
        titleText.color = Color.yellow;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        if (messageFont != null) titleText.font = messageFont;

        // Tạo đường kẻ ngăn cách tiêu đề (Separator)
        GameObject sepObj = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sepObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform sepRect = sepObj.GetComponent<RectTransform>();
        sepRect.anchoredPosition = new Vector2(0f, 65f);
        sepRect.sizeDelta = new Vector2(400f, 1.5f);
        Image sepImg = sepObj.GetComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Hướng dẫn điều khiển chi tiết dạng Rich Text
        GameObject controlsObj = new GameObject("ControlsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        controlsObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform controlsRect = controlsObj.GetComponent<RectTransform>();
        controlsRect.anchoredPosition = new Vector2(0f, -5f);
        controlsRect.sizeDelta = new Vector2(420f, 120f);
        TextMeshProUGUI controlsText = controlsObj.GetComponent<TextMeshProUGUI>();
        controlsText.text = "<color=#FFCC00><b>MOVE:</b></color>  WASD or Arrow Keys\n" +
                            "<color=#FFCC00><b>FIRE:</b></color>  Spacebar\n\n" +
                            "<color=#00E5FF><i>Dodge obstacles and eliminate invaders!</i></color>";
        controlsText.fontSize = 16;
        controlsText.color = Color.white;
        controlsText.alignment = TextAlignmentOptions.Center;
        if (messageFont != null) controlsText.font = messageFont;

        // Dòng nhắc ấn nút để bắt đầu
        GameObject startObj = new GameObject("StartText", typeof(RectTransform), typeof(TextMeshProUGUI));
        startObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform startRect = startObj.GetComponent<RectTransform>();
        startRect.anchoredPosition = new Vector2(0f, -100f);
        startRect.sizeDelta = new Vector2(420f, 30f);
        TextMeshProUGUI startText = startObj.GetComponent<TextMeshProUGUI>();
        startText.text = "► PRESS ANY KEY TO DEPLOY ◄";
        startText.fontSize = 14;
        startText.color = new Color(1f, 0.45f, 0f);
        startText.alignment = TextAlignmentOptions.Center;
        startText.fontStyle = FontStyles.Bold;
        if (messageFont != null) startText.font = messageFont;

        // Hiệu ứng nhấp nháy chữ
        StartCoroutine(PulsateText(startRect));

        // Bắt đầu chờ tương tác phím để đóng Panel
        StartCoroutine(DismissTutorialRoutine(tutorialPanel.GetComponent<CanvasGroup>()));
    }

    private IEnumerator PulsateText(RectTransform rect)
    {
        while (rect != null)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f;
            rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    private IEnumerator DismissTutorialRoutine(CanvasGroup group)
    {
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return null;

        while (!Input.anyKeyDown)
        {
            yield return null;
        }

        Time.timeScale = oldTimeScale;

        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (group != null) group.alpha = 1f - (elapsed / duration);
            yield return null;
        }

        if (group != null)
        {
            Destroy(group.gameObject);
        }
    }

    private void EnsureHUDSprites(Slider slider, string fillPath)
    {
        if (slider == null) return;

        // Hide default circular handle
        Transform handleArea = slider.transform.Find("Handle Slide Area");
        if (handleArea != null) handleArea.gameObject.SetActive(false);

        // Set Background image sprite (ui_bar_bg.png)
        Transform bgTransform = slider.transform.Find("Background");
        if (bgTransform != null)
        {
            Image bgImg = bgTransform.GetComponent<Image>();
            if (bgImg != null)
            {
                Sprite bgSprite = LoadSpriteRuntime("Assets/Sprites/UI/ui_bar_bg.png");
                if (bgSprite != null)
                {
                    bgImg.sprite = bgSprite;
                    bgImg.type = Image.Type.Sliced;
                }
            }
        }

        // Set Fill image sprite
        Transform fillArea = slider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fillTransform = fillArea.Find("Fill");
            if (fillTransform != null)
            {
                Image fillImg = fillTransform.GetComponent<Image>();
                if (fillImg != null)
                {
                    Sprite fillSprite = LoadSpriteRuntime(fillPath);
                    if (fillSprite != null)
                    {
                        fillImg.sprite = fillSprite;
                        fillImg.type = Image.Type.Sliced;
                    }
                }
            }
        }
    }

    private Sprite LoadSpriteRuntime(string relativePath)
    {
        // 1. Try loading from the centralized SpriteDatabase first
        if (SpriteDatabase.Instance != null)
        {
            Sprite loaded = SpriteDatabase.Instance.GetSprite(relativePath);
            if (loaded != null) return loaded;
        }

#if UNITY_EDITOR
        // 2. Editor Fallback: Load raw file bytes from disk
        string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath.Replace("Assets/", ""));
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    tex.filterMode = FilterMode.Point;
                    // Define 9-slice border (24 pixels from each edge)
                    Vector4 border = new Vector4(24f, 24f, 24f, 24f);
                    return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect, border);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[HUDController] Dynamic sprite load failed: " + e.Message);
            }
        }
#endif
        return null;
    }

    private void UpdateHP(int hp)
    {
        if (HPSlider != null)
        {
            if (playerHealth == null) playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            float maxHP = playerHealth != null ? playerHealth.maxHP : 100f;
            HPSlider.value = (maxHP > 0) ? ((float)hp / maxHP) * 100f : 0f;
        }
    }

    private void UpdateShield(int shield)
    {
        if (ShieldSlider != null)
        {
            if (playerHealth == null) playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            float maxShield = playerHealth != null ? playerHealth.maxShield : 100f;
            ShieldSlider.value = (maxShield > 0) ? ((float)shield / maxShield) * 100f : 0f;
        }
    }

    private void UpdateScore(int score)
    {
        if (_vfx != null)
            _vfx.AnimateScore(score);
        else if (ScoreTextTMP != null)
            ScoreTextTMP.text = $"SCORE: {score}";
    }

    private void UpdateWave(int wave)
    {
        if (_vfx != null)
            _vfx.AnimateWave(wave, $"WAVES: {wave}");
        else if (WaveTextTMP != null)
            WaveTextTMP.text = $"WAVES: {wave}";
    }

    private void UpdateEnemyCount(int remaining)
    {
        if (WaveManager.Instance != null && WaveManager.Instance.GetState() == WaveState.Battling)
        {
            if (_vfx != null)
                _vfx.AnimateWave(remaining, $"ENEMIES: {remaining}");
            else if (WaveTextTMP != null)
                WaveTextTMP.text = $"ENEMIES: {remaining}";
        }
    }

    private void UpdateLives(int lives)
    {
        if (_vfx != null)
            _vfx.AnimateLives(lives);
        else if (LivesTextTMP != null)
            LivesTextTMP.text = $"LIVES: {lives}";
    }

    /// <summary>Displays a temporary full-screen message (e.g., "LEVEL COMPLETE").</summary>
    public void DisplayMessage(string message, float duration)
    {
        GameObject textObj = new GameObject("HUDMessageText");
        textObj.transform.SetParent(transform, false); // Parent to this Canvas

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800f, 150f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 48;
        tmp.color = Color.yellow;
        tmp.alignment = TextAlignmentOptions.Center;

        if (messageFont != null) tmp.font = messageFont;

        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        Destroy(textObj, duration);
    }
}
