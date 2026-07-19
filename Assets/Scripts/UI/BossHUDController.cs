using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHUDController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject warningBanner;
    public GameObject bossHUDPanel;
    public Slider bossHPSlider;
    public Slider damageBufferSlider;
    
    [Header("Transitions")]
    public float hpLerpSpeed = 10f;       // Speed at which the main red HP bar slides left
    public float bufferLerpSpeed = 2f;    // Speed at which the orange buffer bar lags behind

    private BossController bossInstance;
    private EnemyHealth bossHealth;

    private float targetHPNormalized = 1f;

    private void Awake()
    {
        if (warningBanner != null) warningBanner.SetActive(false);
        if (bossHUDPanel != null) bossHUDPanel.SetActive(false);
    }

    private void Update()
    {
        if (bossHUDPanel != null && bossHUDPanel.activeSelf)
        {
            // Smoothly lerp main health bar to target (using unscaled time to avoid slow-motion freeze)
            if (bossHPSlider != null)
            {
                bossHPSlider.value = Mathf.Lerp(bossHPSlider.value, targetHPNormalized, Time.unscaledDeltaTime * hpLerpSpeed);
                if (Mathf.Abs(bossHPSlider.value - targetHPNormalized) < 0.1f)
                {
                    bossHPSlider.value = targetHPNormalized;
                }

                // Unity UI Slider quirk: hide fill rect completely when value is close to zero
                if (bossHPSlider.fillRect != null)
                {
                    bossHPSlider.fillRect.gameObject.SetActive(bossHPSlider.value > 0.01f);
                }
            }

            // Smoothly lerp damage buffer bar to main health bar (using unscaled time)
            if (damageBufferSlider != null && bossHPSlider != null)
            {
                if (damageBufferSlider.value > bossHPSlider.value)
                {
                    damageBufferSlider.value = Mathf.Lerp(damageBufferSlider.value, bossHPSlider.value, Time.unscaledDeltaTime * bufferLerpSpeed);
                    if (damageBufferSlider.value - bossHPSlider.value < 0.1f)
                    {
                        damageBufferSlider.value = bossHPSlider.value;
                    }
                }
                else
                {
                    damageBufferSlider.value = bossHPSlider.value;
                }

                // Unity UI Slider quirk: hide fill rect completely when value is close to zero
                if (damageBufferSlider.fillRect != null)
                {
                    damageBufferSlider.fillRect.gameObject.SetActive(damageBufferSlider.value > 0.01f);
                }
            }

            // Pulsate Boss HUD if low health (less than 25%) using unscaled time
            if (bossHPSlider != null && targetHPNormalized < 25f)
            {
                float pulse = 1.0f + Mathf.Sin(Time.unscaledTime * 10f) * 0.02f;
                bossHUDPanel.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
            else if (bossHUDPanel != null)
            {
                bossHUDPanel.transform.localScale = Vector3.one;
            }
        }
    }

    public void ShowWarning(float duration)
    {
        StartCoroutine(WarningRoutine(duration));
    }

    private IEnumerator WarningRoutine(float duration)
    {
        if (warningBanner != null) warningBanner.SetActive(true);
        if (bossHUDPanel != null) bossHUDPanel.SetActive(false); // Ensure hidden during warning
        
        yield return new WaitForSeconds(duration);
        
        if (warningBanner != null) warningBanner.SetActive(false);
        if (bossHUDPanel != null) bossHUDPanel.SetActive(true);

        // Find the boss in the scene dynamically
        bossInstance = Object.FindAnyObjectByType<BossController>();
        if (bossInstance != null)
        {
            // Subscribe to BossController events
            bossInstance.OnBossDead.RemoveListener(DeactivateBossHPBar);
            bossInstance.OnBossDead.AddListener(DeactivateBossHPBar);

            bossInstance.OnPhaseChanged.RemoveListener(OnBossPhaseChanged);
            bossInstance.OnPhaseChanged.AddListener(OnBossPhaseChanged);

            bossHealth = bossInstance.GetComponent<EnemyHealth>();
            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged.RemoveListener(UpdateHP);
                bossHealth.OnHealthChanged.AddListener(UpdateHP);
                
                // Initialize health values for bars to percentage (0 -> 100)
                if (bossHPSlider != null) bossHPSlider.maxValue = 100f;
                if (damageBufferSlider != null) damageBufferSlider.maxValue = 100f;

                targetHPNormalized = ((float)bossHealth.CurrentHP / bossHealth.maxHP) * 100f;
                if (bossHPSlider != null)
                {
                    bossHPSlider.value = targetHPNormalized;
                    if (bossHPSlider.fillRect != null) bossHPSlider.fillRect.gameObject.SetActive(bossHPSlider.value > 0.01f);
                }
                if (damageBufferSlider != null)
                {
                    damageBufferSlider.value = targetHPNormalized;
                    if (damageBufferSlider.fillRect != null) damageBufferSlider.fillRect.gameObject.SetActive(damageBufferSlider.value > 0.01f);
                }
            }
        }
    }

    private void OnBossPhaseChanged(int phase)
    {
        // When phase changes, refresh the HP bar target based on current HP ratio
        if (bossHealth != null)
        {
            UpdateHP(bossHealth.CurrentHP);
        }
    }

    private void UpdateHP(int hp)
    {
        if (bossHealth != null)
        {
            float prevHP = targetHPNormalized;
            targetHPNormalized = ((float)hp / bossHealth.maxHP) * 100f;

            // If damage taken, pulse the whole HUD panel
            if (targetHPNormalized < prevHP)
            {
                StopCoroutine("PulseHUD");
                StartCoroutine(PulseHUD());
            }

            // If it's the start (full health) or target is reset to full, snap values immediately
            if (hp == bossHealth.maxHP)
            {
                if (bossHPSlider != null)
                {
                    bossHPSlider.value = targetHPNormalized;
                    if (bossHPSlider.fillRect != null) bossHPSlider.fillRect.gameObject.SetActive(bossHPSlider.value > 0.01f);
                }
                if (damageBufferSlider != null)
                {
                    damageBufferSlider.value = targetHPNormalized;
                    if (damageBufferSlider.fillRect != null) damageBufferSlider.fillRect.gameObject.SetActive(damageBufferSlider.value > 0.01f);
                }
            }
        }
    }

    private IEnumerator PulseHUD()
    {
        if (bossHUDPanel == null) yield break;
        Vector3 originalScale = Vector3.one;
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float s = 1.0f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.05f;
            bossHUDPanel.transform.localScale = originalScale * s;
            yield return null;
        }
        bossHUDPanel.transform.localScale = originalScale;
    }

    private Coroutine deactivateRoutine;

    private void DeactivateBossHPBar()
    {
        targetHPNormalized = 0f; // Force target to 0 on death so it drains completely
        
        if (deactivateRoutine != null) StopCoroutine(deactivateRoutine);
        if (gameObject.activeInHierarchy)
        {
            deactivateRoutine = StartCoroutine(DelayDeactivateHUD());
        }
        else
        {
            if (bossHUDPanel != null) bossHUDPanel.SetActive(false);
        }
    }

    private IEnumerator DelayDeactivateHUD()
    {
        // Wait until both the HP bar and buffer bar are fully drained to 0 (using unscaled time)
        float timeout = 2.5f;
        while (timeout > 0f)
        {
            float hpVal = bossHPSlider != null ? bossHPSlider.value : 0f;
            float bufVal = damageBufferSlider != null ? damageBufferSlider.value : 0f;

            if (hpVal <= 0.1f && bufVal <= 0.1f)
            {
                break;
            }

            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // Ensure both sliders are completely zeroed out and their fills hidden
        if (bossHPSlider != null)
        {
            bossHPSlider.value = 0f;
            if (bossHPSlider.fillRect != null) bossHPSlider.fillRect.gameObject.SetActive(false);
        }
        if (damageBufferSlider != null)
        {
            damageBufferSlider.value = 0f;
            if (damageBufferSlider.fillRect != null) damageBufferSlider.fillRect.gameObject.SetActive(false);
        }

        // Wait another 0.5 seconds of real time for dramatic effect
        yield return new WaitForSecondsRealtime(0.5f);

        if (bossHUDPanel != null) bossHUDPanel.SetActive(false);
    }

    public void HideBossHUD()
    {
        DeactivateBossHPBar();
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged.RemoveListener(UpdateHP);
        }
        if (bossInstance != null)
        {
            bossInstance.OnBossDead.RemoveListener(DeactivateBossHPBar);
            bossInstance.OnPhaseChanged.RemoveListener(OnBossPhaseChanged);
        }
    }

    private void Start()
    {
        RedesignHUD();
    }

    private void RedesignHUD()
    {
        Sprite barBgSprite = LoadSpriteRuntime("Assets/Sprites/UI/ui_bar_bg.png");
        Sprite barFillSprite = LoadSpriteRuntime("Assets/Sprites/UI/ui_healthbar_fill.png");

        // 1. Redesign warningBanner
        if (warningBanner != null)
        {
            RectTransform warningRect = warningBanner.GetComponent<RectTransform>();
            if (warningRect != null)
            {
                warningRect.anchorMin = new Vector2(0.5f, 0.5f);
                warningRect.anchorMax = new Vector2(0.5f, 0.5f);
                warningRect.pivot = new Vector2(0.5f, 0.5f);
                warningRect.anchoredPosition = new Vector2(0f, 50f);
                warningRect.sizeDelta = new Vector2(680f, 130f);
            }

            Image warningBg = warningBanner.GetComponent<Image>();
            if (warningBg == null) warningBg = warningBanner.AddComponent<Image>();
            if (warningBg != null)
            {
                warningBg.sprite = barBgSprite;
                warningBg.type = Image.Type.Sliced;
                warningBg.color = new Color(0.18f, 0.02f, 0.02f, 0.95f); // Red warning glassmorphism
            }

            TMP_Text[] warningTexts = warningBanner.GetComponentsInChildren<TMP_Text>(true);
            if (warningTexts != null && warningTexts.Length > 0)
            {
                foreach (var text in warningTexts)
                {
                    text.alignment = TextAlignmentOptions.Center;
                    if (text.gameObject.name.Contains("Title") || text.text.Contains("WARNING"))
                    {
                        text.fontSize = 28;
                        text.fontStyle = FontStyles.Bold;
                        text.color = new Color(1f, 0.1f, 0.1f);
                        text.text = "► H A Z A R D  W A R N I N G ◄";
                    }
                    else
                    {
                        text.fontSize = 13;
                        text.color = new Color(1f, 0.7f, 0f);
                        text.text = "MASSIVE ENERGY SIGNATURE DETECTED AHEAD";
                    }
                }
            }
        }

        // 2. Redesign bossHUDPanel
        if (bossHUDPanel != null)
        {
            RectTransform hudRect = bossHUDPanel.GetComponent<RectTransform>();
            if (hudRect != null)
            {
                hudRect.anchorMin = new Vector2(0.5f, 1f);
                hudRect.anchorMax = new Vector2(0.5f, 1f);
                hudRect.pivot = new Vector2(0.5f, 1f);
                hudRect.anchoredPosition = new Vector2(0f, -40f);
                hudRect.sizeDelta = new Vector2(620f, 85f);
            }

            Image hudBg = bossHUDPanel.GetComponent<Image>();
            if (hudBg == null) hudBg = bossHUDPanel.AddComponent<Image>();
            if (hudBg != null)
            {
                hudBg.sprite = barBgSprite;
                hudBg.type = Image.Type.Sliced;
                hudBg.color = new Color(0.04f, 0.04f, 0.08f, 0.92f); // Glassmorphism container
            }

            // Style Boss Name text
            TMP_Text bossNameText = bossHUDPanel.GetComponentInChildren<TMP_Text>(true);
            if (bossNameText != null)
            {
                RectTransform nameRect = bossNameText.GetComponent<RectTransform>();
                if (nameRect != null)
                {
                    nameRect.anchorMin = new Vector2(0.5f, 1f);
                    nameRect.anchorMax = new Vector2(0.5f, 1f);
                    nameRect.pivot = new Vector2(0.5f, 1f);
                    nameRect.anchoredPosition = new Vector2(0f, -12f);
                    nameRect.sizeDelta = new Vector2(580f, 25f);
                }
                bossNameText.fontSize = 15;
                bossNameText.fontStyle = FontStyles.Bold;
                bossNameText.color = new Color(1f, 0.85f, 0f);
                bossNameText.alignment = TextAlignmentOptions.Center;
                bossNameText.characterSpacing = 8f; // Sci-fi loose spacing
            }

            // 3. Style Sliders
            ConfigureSlider(damageBufferSlider, barBgSprite, barFillSprite, new Color(1f, 0.55f, 0f, 0.9f), new Color(0.06f, 0.06f, 0.1f, 0.8f));
            ConfigureSlider(bossHPSlider, null, barFillSprite, new Color(0.95f, 0.1f, 0.1f, 1f), Color.clear);

            // Overlay HP slider on top of Buffer slider
            if (damageBufferSlider != null && bossHPSlider != null)
            {
                RectTransform bufRect = damageBufferSlider.GetComponent<RectTransform>();
                RectTransform hpRect = bossHPSlider.GetComponent<RectTransform>();

                Vector2 sliderSize = new Vector2(560f, 18f);
                Vector2 sliderPos = new Vector2(0f, 15f);

                if (bufRect != null)
                {
                    bufRect.anchorMin = new Vector2(0.5f, 0f);
                    bufRect.anchorMax = new Vector2(0.5f, 0f);
                    bufRect.pivot = new Vector2(0.5f, 0f);
                    bufRect.anchoredPosition = sliderPos;
                    bufRect.sizeDelta = sliderSize;
                }

                if (hpRect != null)
                {
                    hpRect.anchorMin = new Vector2(0.5f, 0f);
                    hpRect.anchorMax = new Vector2(0.5f, 0f);
                    hpRect.pivot = new Vector2(0.5f, 0f);
                    hpRect.anchoredPosition = sliderPos;
                    hpRect.sizeDelta = sliderSize;
                }
            }
        }
    }

    private void ConfigureSlider(Slider slider, Sprite bgSprite, Sprite fillSprite, Color fillColor, Color bgColor)
    {
        if (slider == null) return;

        // Hide handle slide area
        Transform handleArea = slider.transform.Find("Handle Slide Area");
        if (handleArea != null) handleArea.gameObject.SetActive(false);

        // Configure Background
        Transform bgTransform = slider.transform.Find("Background");
        if (bgTransform != null)
        {
            Image bgImg = bgTransform.GetComponent<Image>();
            if (bgImg != null)
            {
                if (bgSprite != null)
                {
                    bgImg.sprite = bgSprite;
                    bgImg.type = Image.Type.Sliced;
                    bgImg.color = bgColor;
                }
                else
                {
                    bgImg.color = bgColor;
                }
            }
        }

        // Configure Fill Area
        Transform fillArea = slider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            if (fillAreaRect != null)
            {
                fillAreaRect.offsetMin = Vector2.zero;
                fillAreaRect.offsetMax = Vector2.zero;
            }

            Transform fillTransform = fillArea.Find("Fill");
            if (fillTransform != null)
            {
                RectTransform fillRect = fillTransform.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    fillRect.offsetMin = Vector2.zero;
                    fillRect.offsetMax = Vector2.zero;
                }

                Image fillImg = fillTransform.GetComponent<Image>();
                if (fillImg != null)
                {
                    if (fillSprite != null)
                    {
                        fillImg.sprite = fillSprite;
                        fillImg.type = Image.Type.Sliced;
                    }
                    fillImg.color = fillColor;
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
                Debug.LogError("[BossHUDController] Dynamic sprite load failed: " + e.Message);
            }
        }
#endif
        return null;
    }
}


