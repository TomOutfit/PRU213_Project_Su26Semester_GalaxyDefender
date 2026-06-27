using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        // Auto-resolve components if not set in Inspector
        if (HPSlider == null) HPSlider = transform.Find("HPBar")?.GetComponent<Slider>();
        if (ShieldSlider == null) ShieldSlider = transform.Find("ShieldBar")?.GetComponent<Slider>();
        
        if (ScoreTextTMP == null) ScoreTextTMP = transform.Find("ScorePanel/ScoreText")?.GetComponent<TMP_Text>();
        if (WaveTextTMP == null) WaveTextTMP = transform.Find("WavePanel/WaveText")?.GetComponent<TMP_Text>();
        if (LivesTextTMP == null) LivesTextTMP = transform.Find("LivesPanel/LivesText")?.GetComponent<TMP_Text>();

        // Apply visual bar frame sprites dynamically for a polished look
        EnsureHUDSprites(HPSlider, "Assets/Sprites/UI/ui_healthbar_fill.png");
        EnsureHUDSprites(ShieldSlider, "Assets/Sprites/UI/ui_shieldbar_fill.png");

        // Dynamically attach HUDVfx if missing
        if (GetComponent<HUDVfx>() == null)
        {
            HUDVfx vfx = gameObject.AddComponent<HUDVfx>();
            vfx.HPSlider = HPSlider;
            vfx.ShieldSlider = ShieldSlider;
            
            if (ScoreTextTMP != null) vfx.scoreTextRect = ScoreTextTMP.GetComponent<RectTransform>();
            if (WaveTextTMP != null) vfx.waveTextRect = WaveTextTMP.GetComponent<RectTransform>();
            if (LivesTextTMP != null) vfx.livesTextRect = LivesTextTMP.GetComponent<RectTransform>();
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
        if (ScoreTextTMP != null) ScoreTextTMP.text = $"SCORE: {score}";
    }

    private void UpdateWave(int wave)
    {
        if (WaveTextTMP != null) WaveTextTMP.text = $"WAVE: {wave}";
    }

    private void UpdateLives(int lives)
    {
        if (LivesTextTMP != null) LivesTextTMP.text = $"LIVES: {lives}";
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
