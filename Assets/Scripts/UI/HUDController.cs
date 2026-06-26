using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Sliders")]
    public Slider HPSlider;
    public Slider ShieldSlider;

    [Header("Text Elements")]
    public Text ScoreText;
    public Text WaveText;
    public Text LivesText;

    [Header("TMP Text Elements (Optional)")]
    public TMP_Text ScoreTextTMP;
    public TMP_Text WaveTextTMP;
    public TMP_Text LivesTextTMP;

    private void Awake()
    {
        // Subscribe to PlayerHealth events
        PlayerHealth playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHPChanged.AddListener(UpdateHP);
            playerHealth.OnShieldChanged.AddListener(UpdateShield);
            
            // Set initial values
            UpdateHP(playerHealth.currentHP);
            UpdateShield(playerHealth.currentShield);
        }
        else
        {
            Debug.LogWarning("HUDController: PlayerHealth not found in scene during Awake.");
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
            UpdateWave(WaveManager.Instance.GetCurrentWave());
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
        // Double check in case PlayerHealth or Managers were instantiated later
        if (Object.FindAnyObjectByType<PlayerHealth>() != null)
        {
            PlayerHealth playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            playerHealth.OnHPChanged.RemoveListener(UpdateHP);
            playerHealth.OnShieldChanged.RemoveListener(UpdateShield);
            playerHealth.OnHPChanged.AddListener(UpdateHP);
            playerHealth.OnShieldChanged.AddListener(UpdateShield);
            UpdateHP(playerHealth.currentHP);
            UpdateShield(playerHealth.currentShield);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.RemoveListener(UpdateScore);
            ScoreManager.Instance.OnScoreChanged.AddListener(UpdateScore);
            UpdateScore(ScoreManager.Instance.currentScore);
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged.RemoveListener(UpdateWave);
            WaveManager.Instance.OnWaveChanged.AddListener(UpdateWave);
            UpdateWave(WaveManager.Instance.GetCurrentWave());
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged.RemoveListener(UpdateLives);
            GameManager.Instance.OnLivesChanged.AddListener(UpdateLives);
            UpdateLives(GameManager.Instance.GetCurrentLives());
        }

        // Auto-resolve sliders if not set in Inspector
        if (HPSlider == null) HPSlider = transform.Find("HPSlider")?.GetComponent<Slider>();
        if (ShieldSlider == null) ShieldSlider = transform.Find("ShieldSlider")?.GetComponent<Slider>();
        
        // Auto-resolve Text/TMP components
        if (ScoreText == null) ScoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();
        if (WaveText == null) WaveText = GameObject.Find("WaveText")?.GetComponent<Text>();
        if (LivesText == null) LivesText = GameObject.Find("LivesText")?.GetComponent<Text>();

        if (ScoreTextTMP == null) ScoreTextTMP = GameObject.Find("ScoreTextTMP")?.GetComponent<TMP_Text>();
        if (WaveTextTMP == null) WaveTextTMP = GameObject.Find("WaveTextTMP")?.GetComponent<TMP_Text>();
        if (LivesTextTMP == null) LivesTextTMP = GameObject.Find("LivesTextTMP")?.GetComponent<TMP_Text>();

        // Apply visual bar frame sprites dynamically
        EnsureHUDSprites(HPSlider, "Assets/Sprites/UI/ui_healthbar_fill.png");
        EnsureHUDSprites(ShieldSlider, "Assets/Sprites/UI/ui_shieldbar_fill.png");

        // Wrap score, wave, and lives texts in the retro frame sprite dynamically
        WrapTextInFrame("ScoreText");
        WrapTextInFrame("WaveText");
        WrapTextInFrame("LivesText");
        WrapTextInFrame("ScoreTextTMP");
        WrapTextInFrame("WaveTextTMP");

        // Dynamically attach HUDVfx
        if (GetComponent<HUDVfx>() == null)
        {
            HUDVfx vfx = gameObject.AddComponent<HUDVfx>();
            vfx.HPSlider = HPSlider;
            vfx.ShieldSlider = ShieldSlider;

            // Wire up the text rects for scale pulsing animations
            GameObject scoreGO = GameObject.Find("ScoreText");
            if (scoreGO != null) vfx.scoreTextRect = scoreGO.GetComponent<RectTransform>();
            GameObject waveGO = GameObject.Find("WaveText");
            if (waveGO != null) vfx.waveTextRect = waveGO.GetComponent<RectTransform>();
        }
    }

    private void WrapTextInFrame(string textObjectName)
    {
        GameObject textGO = GameObject.Find(textObjectName);
        if (textGO == null)
        {
            Transform t = transform.Find(textObjectName);
            if (t != null) textGO = t.gameObject;
        }

        if (textGO == null) return;

        // Skip if already wrapped
        if (textGO.transform.parent != null && textGO.transform.parent.name.EndsWith("_Frame"))
        {
            return;
        }

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        if (textRect == null) return;

        Sprite frameSprite = LoadSpriteRuntime("Assets/Sprites/UI/ui_bar_bg.png");
        if (frameSprite == null) return;

        // 1. Create Frame GameObject
        GameObject frameGO = new GameObject(textObjectName + "_Frame", typeof(RectTransform), typeof(Image));
        frameGO.transform.SetParent(textRect.parent, false);

        RectTransform frameRect = frameGO.GetComponent<RectTransform>();
        Image frameImg = frameGO.GetComponent<Image>();

        frameImg.sprite = frameSprite;
        frameImg.type = Image.Type.Sliced;
        frameImg.color = Color.white;

        // 2. Position and size frame
        frameRect.anchorMin = textRect.anchorMin;
        frameRect.anchorMax = textRect.anchorMax;
        frameRect.pivot = textRect.pivot;
        frameRect.anchoredPosition = textRect.anchoredPosition;
        frameRect.sizeDelta = new Vector2(260f, 55f);

        // 3. Parent text to frame
        textRect.SetParent(frameRect, false);
        
        // Stretch text with padding inside frame
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(25f, 5f);
        textRect.offsetMax = new Vector2(-25f, -5f);

        // 4. Style text for optimal look inside frame
        Text uiText = textGO.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.fontSize = 20;
            uiText.color = new Color(0.9f, 0.95f, 1f, 1f); // Neon light blue
        }

        TMP_Text tmpText = textGO.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 20;
            tmpText.color = new Color(0.9f, 0.95f, 1f, 1f);
        }
    }

    private void EnsureHUDSprites(Slider slider, string fillPath)
    {
        if (slider == null) return;

        // 1. Hide default circular handle
        Transform handleArea = slider.transform.Find("Handle Slide Area");
        if (handleArea != null)
        {
            handleArea.gameObject.SetActive(false);
        }

        // 2. Set Background image sprite (ui_bar_bg.png)
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
                    bgImg.color = Color.white;
                }
            }
        }

        // 3. Set Fill image sprite and adjust inner padding for 9-slice nesting
        Transform fillArea = slider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            if (fillAreaRect != null)
            {
                fillAreaRect.anchorMin = new Vector2(0.06f, 0.14f);
                fillAreaRect.anchorMax = new Vector2(0.94f, 0.86f);
                fillAreaRect.offsetMin = Vector2.zero;
                fillAreaRect.offsetMax = Vector2.zero;
            }

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
                        fillImg.color = Color.white;
                    }
                }

                RectTransform fillRect = fillTransform.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    fillRect.offsetMin = Vector2.zero;
                    fillRect.offsetMax = Vector2.zero;
                }
            }
        }
    }

    private Sprite LoadSpriteRuntime(string relativePath)
    {
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
                    Vector4 border = new Vector4(24f, 24f, 24f, 24f); // 9-slice border definition
                    return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect, border);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[HUDController] Dynamic sprite load failed: " + e.Message);
            }
        }
        return null;
    }

    private void UpdateHP(int hp)
    {
        if (HPSlider != null) HPSlider.value = hp;
    }

    private void UpdateShield(int shield)
    {
        if (ShieldSlider != null) ShieldSlider.value = shield;
    }

    private void UpdateScore(int score)
    {
        string text = "Score: " + score;
        if (ScoreText != null) ScoreText.text = text;
        if (ScoreTextTMP != null) ScoreTextTMP.text = text;
    }

    private void UpdateWave(int wave)
    {
        string text = "Wave: " + wave;
        if (WaveText != null) WaveText.text = text;
        if (WaveTextTMP != null) WaveTextTMP.text = text;
    }

    private void UpdateLives(int lives)
    {
        string text = "Lives: " + lives;
        if (LivesText != null) LivesText.text = text;
        if (LivesTextTMP != null) LivesTextTMP.text = text;
    }

    [Header("Message Settings")]
    public TMPro.TMP_FontAsset messageFont;

    public void DisplayMessage(string message, float duration)
    {
        GameObject canvasHUD = GameObject.Find("Canvas_HUD");
        if (canvasHUD == null) return;

        GameObject textObj = new GameObject("HUDMessageText");
        textObj.transform.SetParent(canvasHUD.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800f, 150f);

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 48;
        tmp.color = Color.yellow;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        if (messageFont != null)
        {
            tmp.font = messageFont;
        }

        tmp.fontStyle = TMPro.FontStyles.Normal;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        Destroy(textObj, duration);
    }
}
