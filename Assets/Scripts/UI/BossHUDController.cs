using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
            // Smoothly lerp main health bar to target
            if (bossHPSlider != null)
            {
                bossHPSlider.value = Mathf.Lerp(bossHPSlider.value, targetHPNormalized, Time.deltaTime * hpLerpSpeed);
                if (Mathf.Abs(bossHPSlider.value - targetHPNormalized) < 0.1f)
                {
                    bossHPSlider.value = targetHPNormalized;
                }
            }

            // Smoothly lerp damage buffer bar to main health bar
            if (damageBufferSlider != null && bossHPSlider != null)
            {
                if (damageBufferSlider.value > bossHPSlider.value)
                {
                    damageBufferSlider.value = Mathf.Lerp(damageBufferSlider.value, bossHPSlider.value, Time.deltaTime * bufferLerpSpeed);
                    if (damageBufferSlider.value - bossHPSlider.value < 0.1f)
                    {
                        damageBufferSlider.value = bossHPSlider.value;
                    }
                }
                else
                {
                    damageBufferSlider.value = bossHPSlider.value;
                }
            }

            // Pulsate Boss HUD if low health (less than 25%)
            if (bossHPSlider != null && targetHPNormalized < 25f)
            {
                float pulse = 1.0f + Mathf.Sin(Time.time * 10f) * 0.02f;
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
                if (bossHPSlider != null) bossHPSlider.value = targetHPNormalized;
                if (damageBufferSlider != null) damageBufferSlider.value = targetHPNormalized;
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
                if (bossHPSlider != null) bossHPSlider.value = targetHPNormalized;
                if (damageBufferSlider != null) damageBufferSlider.value = targetHPNormalized;
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
            elapsed += Time.deltaTime;
            float s = 1.0f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.05f;
            bossHUDPanel.transform.localScale = originalScale * s;
            yield return null;
        }
        bossHUDPanel.transform.localScale = originalScale;
    }

    private void DeactivateBossHPBar()
    {
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
}


