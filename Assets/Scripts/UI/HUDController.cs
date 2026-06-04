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

    [Header("TMP Text Elements (Optional)")]
    public TMP_Text ScoreTextTMP;
    public TMP_Text WaveTextTMP;

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
}
