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
                if (Mathf.Abs(bossHPSlider.value - targetHPNormalized) < 0.001f)
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
                    if (damageBufferSlider.value - bossHPSlider.value < 0.001f)
                    {
                        damageBufferSlider.value = bossHPSlider.value;
                    }
                }
                else
                {
                    damageBufferSlider.value = bossHPSlider.value;
                }
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
                
                // Initialize health values for bars
                targetHPNormalized = (float)bossHealth.CurrentHP / bossHealth.maxHP;
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
            targetHPNormalized = (float)hp / bossHealth.maxHP;

            // If it's the start (full health) or target is reset to full, snap values immediately
            if (hp == bossHealth.maxHP)
            {
                if (bossHPSlider != null) bossHPSlider.value = targetHPNormalized;
                if (damageBufferSlider != null) damageBufferSlider.value = targetHPNormalized;
            }
        }
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


