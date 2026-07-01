using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class VictoryController : MonoBehaviour
{
    [Header("Stats Text (TMP)")]
    public TMP_Text scoreTextTMP;
    public TMP_Text survivalTimeTextTMP;
    public TMP_Text enemiesKilledTextTMP;

    [Header("Buttons")]
    public Button menuButton;
    public Button playAgainButton;

    [Header("Title Pulse")]
    public TMP_Text titleText;
    public float pulseScale  = 1.12f;
    public float pulseSpeed  = 2.5f;

    private void Awake()
    {
        if (menuButton == null)
            menuButton = GameObject.Find("MenuButton")?.GetComponent<Button>();
        if (playAgainButton == null)
            playAgainButton = GameObject.Find("PlayAgainButton")?.GetComponent<Button>();
        if (titleText == null)
            titleText = GameObject.Find("TitleText")?.GetComponent<TMP_Text>();

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuClick);
        }
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(OnPlayAgainClick);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        int finalScore  = ScoreManager.Instance != null ? ScoreManager.Instance.currentScore : 0;
        float timeVal   = GameManager.Instance   != null ? GameManager.Instance.survivalTime  : 0f;
        int killedCount = GameManager.Instance   != null ? GameManager.Instance.totalEnemiesKilled : 0;

        int minutes = Mathf.FloorToInt(timeVal / 60f);
        int seconds = Mathf.FloorToInt(timeVal % 60f);
        string timeStr = $"{minutes}:{seconds:D2}";

        if (scoreTextTMP         != null) scoreTextTMP.text         = $"FINAL SCORE: {finalScore:N0}";
        if (enemiesKilledTextTMP != null) enemiesKilledTextTMP.text = $"ENEMIES DEFEATED: {killedCount}";
        if (survivalTimeTextTMP  != null) survivalTimeTextTMP.text  = $"SURVIVAL TIME: {timeStr}";

        // Save high score
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveHighScore(finalScore);

        // Play BGM
        AudioManager.Instance?.PlayBGM("bgm_winner");

        // Start title pulse animation
        if (titleText != null)
            StartCoroutine(PulseTitle());
    }

    private IEnumerator PulseTitle()
    {
        if (titleText == null) yield break;
        RectTransform rt = titleText.GetComponent<RectTransform>();
        while (true)
        {
            float s = 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
            if (rt != null) rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    public void OnMenuClick()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    public void OnPlayAgainClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetSession();
            GameManager.Instance.SetState(GameManager.State.Playing);
        }

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("Level1");
        else
            SceneManager.LoadScene("Level1");
    }
}
