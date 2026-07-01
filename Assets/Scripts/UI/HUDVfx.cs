using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDVfx : MonoBehaviour
{
    [Header("Sliders")]
    public Slider HPSlider;
    public Slider ShieldSlider;

    [Header("Buffer Bar Settings")]
    public Color hpBufferColor = new Color(1f, 0.5f, 0f, 0.8f);     // Dark orange
    public Color shieldBufferColor = new Color(1f, 0.8f, 0f, 0.8f); // Gold/yellow
    public float bufferDelay = 0.5f;                              // Time before buffer starts dropping
    public float bufferSpeed = 2f;                                // Speed of buffer catch up

    [Header("Texts to Pulse")]
    public RectTransform scoreTextRect;
    public RectTransform waveTextRect;
    public RectTransform livesTextRect;
    public float pulseScale = 1.3f;
    public float pulseDuration = 0.25f;

    [Header("Low Health Glow")]
    public float lowHPThreshold = 30f;
    public float hpPulseSpeed = 8f;
    public Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);

    private RectTransform hpFillRect;
    private RectTransform hpBufferRect;
    private Image hpFillImage;
    private Color hpOriginalColor;

    private RectTransform shieldFillRect;
    private RectTransform shieldBufferRect;

    private float hpBufferValue;
    private float hpTargetValue;
    private float hpBufferTimer;

    private float shieldBufferValue;
    private float shieldTargetValue;
    private float shieldBufferTimer;

    private int lastScore = 0;
    private int lastWave = 0;
    private int lastLives = 0;

    private Sprite whitePixelSprite;

    private void Start()
    {
        // Setup Buffer for HP Slider
        if (HPSlider != null)
        {
            SetupBufferBar(HPSlider, hpBufferColor, ref hpFillRect, ref hpBufferRect);
            hpFillImage = hpFillRect?.GetComponent<Image>();
            if (hpFillImage != null)
            {
                hpOriginalColor = hpFillImage.color;
            }
            hpBufferValue = HPSlider.value;
            hpTargetValue = HPSlider.value;

            // Subscribe to slider value change to trigger buffer delay
            HPSlider.onValueChanged.AddListener(OnHPValueChanged);
        }

        // Setup Buffer for Shield Slider
        if (ShieldSlider != null)
        {
            SetupBufferBar(ShieldSlider, shieldBufferColor, ref shieldFillRect, ref shieldBufferRect);
            shieldBufferValue = ShieldSlider.value;
            shieldTargetValue = ShieldSlider.value;

            ShieldSlider.onValueChanged.AddListener(OnShieldValueChanged);
        }

        // Find Score and Wave Text RectTransforms if not assigned
        HUDController hud = GetComponent<HUDController>();
        if (hud != null)
        {
            if (scoreTextRect == null)
            {
                if (hud.ScoreTextTMP != null) scoreTextRect = hud.ScoreTextTMP.GetComponent<RectTransform>();
            }

            if (waveTextRect == null)
            {
                if (hud.WaveTextTMP != null) waveTextRect = hud.WaveTextTMP.GetComponent<RectTransform>();
            }

            if (livesTextRect == null)
            {
                if (hud.LivesTextTMP != null) livesTextRect = hud.LivesTextTMP.GetComponent<RectTransform>();
            }
        }

        // Setup Score and Wave listeners
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
            lastScore = ScoreManager.Instance.currentScore;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged.AddListener(OnWaveChanged);
            lastWave = WaveManager.Instance.GetCurrentWave();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged.AddListener(OnLivesChanged);
            lastLives = GameManager.Instance.GetCurrentLives();
        }

        // Generate white pixel texture for shield break particles
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.SetPixel(0, 1, Color.white);
        tex.SetPixel(1, 0, Color.white);
        tex.SetPixel(1, 1, Color.white);
        tex.Apply();
        whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private void OnDestroy()
    {
        if (whitePixelSprite != null)
        {
            Destroy(whitePixelSprite.texture);
            Destroy(whitePixelSprite);
        }
    }

    private void SetupBufferBar(Slider slider, Color color, ref RectTransform fillRect, ref RectTransform bufferRect)
    {
        // Find Fill Area (standard child of Slider)
        Transform fillArea = slider.transform.Find("Fill Area");
        if (fillArea == null) return;

        // Find main Fill (child of Fill Area)
        Transform fill = fillArea.Find("Fill");
        if (fill == null) return;

        fillRect = fill.GetComponent<RectTransform>();

        // Create Buffer Fill GameObject
        GameObject bufferGO = Instantiate(fill.gameObject, fillArea);
        bufferGO.name = "BufferFill";
        
        // Put buffer behind main fill in hierarchy so it renders behind
        bufferGO.transform.SetSiblingIndex(0);

        bufferRect = bufferGO.GetComponent<RectTransform>();

        // Change color to buffer color
        Image bufferImg = bufferGO.GetComponent<Image>();
        if (bufferImg != null)
        {
            bufferImg.color = color;
        }
    }

    private void OnHPValueChanged(float value)
    {
        if (value < hpTargetValue)
        {
            // HP decreased, trigger delay for buffer
            hpBufferTimer = bufferDelay;
            // Pulse the bar on damage
            StartCoroutine(PulseBar(HPSlider.transform));
        }
        else if (value > hpTargetValue)
        {
            // HP increased, snap buffer up immediately
            hpBufferValue = value;
            // Also pulse on heal (different effect maybe?)
            StartCoroutine(PulseBar(HPSlider.transform));
        }
        hpTargetValue = value;
    }

    private void OnShieldValueChanged(float value)
    {
        if (value < shieldTargetValue)
        {
            shieldBufferTimer = bufferDelay;
            StartCoroutine(PulseBar(ShieldSlider.transform));

            // If shield just broke (reached 0)
            if (value <= 0f && shieldTargetValue > 0f)
            {
                TriggerShieldBreakParticles();
            }
        }
        else if (value > shieldTargetValue)
        {
            shieldBufferValue = value;
            StartCoroutine(PulseBar(ShieldSlider.transform));
        }
        shieldTargetValue = value;
    }

    private IEnumerator PulseBar(Transform bar)
    {
        Vector3 originalScale = Vector3.one;
        float elapsed = 0f;
        float duration = 0.15f;
        float scaleAmount = 1.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            float currentScale = Mathf.Lerp(1f, scaleAmount, Mathf.Sin(pct * Mathf.PI));
            bar.localScale = originalScale * currentScale;
            yield return null;
        }
        bar.localScale = originalScale;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. HP Buffer Update
        if (HPSlider != null && hpBufferRect != null && hpFillRect != null)
        {
            if (hpBufferTimer > 0f)
            {
                hpBufferTimer -= dt;
            }
            else
            {
                // Smoothly lerp buffer down
                hpBufferValue = Mathf.MoveTowards(hpBufferValue, hpTargetValue, bufferSpeed * HPSlider.maxValue * dt);
            }

            // Sync buffer rect to target ratio
            float hpPct = HPSlider.maxValue > 0 ? hpBufferValue / HPSlider.maxValue : 0;
            hpBufferRect.anchorMax = new Vector2(hpPct, hpBufferRect.anchorMax.y);
            hpBufferRect.sizeDelta = hpFillRect.sizeDelta;
            hpBufferRect.anchoredPosition = hpFillRect.anchoredPosition;
        }

        // 2. Shield Buffer Update
        if (ShieldSlider != null && shieldBufferRect != null && shieldFillRect != null)
        {
            if (shieldBufferTimer > 0f)
            {
                shieldBufferTimer -= dt;
            }
            else
            {
                shieldBufferValue = Mathf.MoveTowards(shieldBufferValue, shieldTargetValue, bufferSpeed * ShieldSlider.maxValue * dt);
            }

            float shieldPct = ShieldSlider.maxValue > 0 ? shieldBufferValue / ShieldSlider.maxValue : 0;
            shieldBufferRect.anchorMax = new Vector2(shieldPct, shieldBufferRect.anchorMax.y);
            shieldBufferRect.sizeDelta = shieldFillRect.sizeDelta;
            shieldBufferRect.anchoredPosition = shieldFillRect.anchoredPosition;
        }

        // 3. Low HP Pulsing Danger Alarm
        if (HPSlider != null && hpFillImage != null)
        {
            float hpPercent = HPSlider.maxValue > 0 ? (HPSlider.value / HPSlider.maxValue) * 100f : 0;
            if (hpPercent <= lowHPThreshold && hpPercent > 0)
            {
                // Pulse color between red and original color
                float pulse = (Mathf.Sin(Time.time * hpPulseSpeed) + 1f) / 2f;
                hpFillImage.color = Color.Lerp(hpOriginalColor, dangerColor, pulse);
                
                // Pulsate scale slightly
                HPSlider.transform.localScale = Vector3.one * (1.0f + pulse * 0.05f);
            }
            else
            {
                hpFillImage.color = hpOriginalColor;
                HPSlider.transform.localScale = Vector3.one;
            }
        }
    }

    private void OnScoreChanged(int score)
    {
        if (score != lastScore && scoreTextRect != null)
        {
            StopCoroutine("PulseText");
            StartCoroutine(PulseText(scoreTextRect));
        }
        lastScore = score;
    }

    private void OnWaveChanged(int wave)
    {
        if (wave != lastWave && waveTextRect != null)
        {
            StopCoroutine("PulseText");
            StartCoroutine(PulseText(waveTextRect));
        }
        lastWave = wave;
    }

    private void OnLivesChanged(int lives)
    {
        if (lives != lastLives && livesTextRect != null)
        {
            StopCoroutine("PulseText");
            StartCoroutine(PulseText(livesTextRect));
        }
        lastLives = lives;
    }

    private IEnumerator PulseText(RectTransform textRect)
    {
        // If the text is parented to a frame, pulse the frame instead
        RectTransform targetRect = textRect;
        if (textRect.parent != null && textRect.parent.name.EndsWith("_Frame"))
        {
            targetRect = textRect.parent.GetComponent<RectTransform>();
        }

        Vector3 originalScale = Vector3.one;
        float elapsed = 0f;

        // Scale up quickly
        float halfDuration = pulseDuration * 0.4f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / halfDuration;
            if (targetRect != null)
            {
                targetRect.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, pct);
            }
            yield return null;
        }

        // Scale back down with a bounce
        elapsed = 0f;
        float returnDuration = pulseDuration * 0.6f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / returnDuration;
            float scaleMult = Mathf.Lerp(pulseScale, 1.0f, pct);
            if (targetRect != null)
            {
                targetRect.localScale = originalScale * scaleMult;
            }
            yield return null;
        }

        if (targetRect != null)
        {
            targetRect.localScale = originalScale;
        }
    }

    private void TriggerShieldBreakParticles()
    {
        if (ShieldSlider == null) return;

        // Find the Shield Slider's center/fill position
        RectTransform sliderRect = ShieldSlider.GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        // Find screen position of shield slider
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, ShieldSlider.transform.position);
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out localPoint);

        // Spawn bright cyan electric shield break particles
        int particleCount = 20;
        Color electricCyan = new Color(0.2f, 0.8f, 1f, 0.9f);

        for (int i = 0; i < particleCount; i++)
        {
            GameObject sparkGO = new GameObject("ShieldSpark", typeof(RectTransform), typeof(Image));
            sparkGO.transform.SetParent(canvasRect.transform, false);

            RectTransform sparkRect = sparkGO.GetComponent<RectTransform>();
            sparkRect.anchoredPosition = localPoint;

            float size = Random.Range(5f, 12f);
            sparkRect.sizeDelta = new Vector2(size, size);

            Image sparkImg = sparkGO.GetComponent<Image>();
            sparkImg.sprite = whitePixelSprite;
            sparkImg.color = electricCyan;
            sparkImg.raycastTarget = false;

            // Electric spark patterns fly sideways and outward
            float angle = Random.Range(-45f, 225f) * Mathf.Deg2Rad;
            float speed = Random.Range(150f, 300f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            StartCoroutine(AnimateShieldSpark(sparkRect, sparkImg, velocity));
        }
    }

    private IEnumerator AnimateShieldSpark(RectTransform sparkRect, Image sparkImg, Vector2 velocity)
    {
        float duration = Random.Range(0.3f, 0.6f);
        float elapsed = 0f;

        Vector3 startScale = sparkRect.localScale;
        Color startCol = sparkImg.color;

        while (elapsed < duration && sparkRect != null)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;

            sparkRect.anchoredPosition += velocity * Time.deltaTime;
            
            // Add some jitter/wave to velocity to look electric
            velocity.x += Random.Range(-100f, 100f) * Time.deltaTime;
            velocity.y += Random.Range(-100f, 100f) * Time.deltaTime;

            sparkRect.localScale = Vector3.Lerp(startScale, Vector3.zero, pct);
            sparkImg.color = Color.Lerp(startCol, new Color(startCol.r, startCol.g, startCol.b, 0f), pct);

            yield return null;
        }

        if (sparkRect != null)
        {
            Destroy(sparkRect.gameObject);
        }
    }
}
