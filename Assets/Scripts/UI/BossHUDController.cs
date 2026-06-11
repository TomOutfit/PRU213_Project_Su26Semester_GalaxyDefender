using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHUDController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject warningBanner;
    public GameObject bossHUDPanel;
    public Slider bossHPSlider;

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
        BossController boss = Object.FindAnyObjectByType<BossController>();
        if (boss != null)
        {
            bossHealth = boss.GetComponent<EnemyHealth>();
            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged.RemoveListener(UpdateHP);
                bossHealth.OnHealthChanged.AddListener(UpdateHP);
                UpdateHP(bossHealth.maxHP); // Initialize starting HP
            }
        }
    }

    private void UpdateHP(int hp)
    {
        if (bossHealth != null && bossHPSlider != null)
        {
            bossHPSlider.maxValue = bossHealth.maxHP;
            bossHPSlider.value = hp;
        }
    }

    public void HideBossHUD()
    {
        if (bossHUDPanel != null) bossHUDPanel.SetActive(false);
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged.RemoveListener(UpdateHP);
        }
    }
}
