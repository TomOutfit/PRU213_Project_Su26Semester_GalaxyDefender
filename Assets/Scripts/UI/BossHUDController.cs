using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHUDController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject warningBanner;
    public GameObject bossHUDPanel;
    public Slider bossHPSlider;

    private BossController bossInstance;
    private EnemyHealth bossHealth;

    private void Awake()
    {
        if (warningBanner != null) warningBanner.SetActive(false);
        if (bossHUDPanel != null) bossHUDPanel.SetActive(false);
    }

    public void ShowWarning(float duration)
    {
        StartCoroutine(WarningRoutine(duration));
    }

    private IEnumerator WarningRoutine(float duration)
    {
        if (warningBanner != null) warningBanner.SetActive(true);
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
                UpdateHP(bossHealth.maxHP); // Initialize starting HP
            }
        }
    }

    private void OnBossPhaseChanged(int phase)
    {
        // Subscribe BossController.OnPhaseChanged -> BossHPSlider.value = hp/maxHP
        if (bossHealth != null && bossHPSlider != null)
        {
            bossHPSlider.value = (float)bossHealth.CurrentHP / bossHealth.maxHP;
        }
    }

    private void UpdateHP(int hp)
    {
        if (bossHealth != null && bossHPSlider != null)
        {
            // Support both direct and normalized slider ranges
            if (bossHPSlider.maxValue > 1.1f)
            {
                bossHPSlider.maxValue = bossHealth.maxHP;
                bossHPSlider.value = hp;
            }
            else
            {
                bossHPSlider.value = (float)hp / bossHealth.maxHP;
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
