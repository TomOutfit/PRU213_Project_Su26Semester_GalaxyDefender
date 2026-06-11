using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverController : MonoBehaviour
{
    [Header("UI Elements")]
    public Text scoreText;
    public Text survivalTimeText;
    public Text enemiesKilledText;

    public TMP_Text scoreTextTMP;
    public TMP_Text survivalTimeTextTMP;
    public TMP_Text enemiesKilledTextTMP;

    [Header("Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    private void Start()
    {
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.currentScore : 0;
        float timeVal = GameManager.Instance != null ? GameManager.Instance.survivalTime : 0f;
        int killedCount = GameManager.Instance != null ? GameManager.Instance.totalEnemiesKilled : 0;

        int minutes = Mathf.FloorToInt(timeVal / 60f);
        int seconds = Mathf.FloorToInt(timeVal % 60f);
        string timeStr = $"Survival Time: {minutes}:{seconds:D2}";

        if (scoreText != null) scoreText.text = $"Score: {finalScore}";
        if (scoreTextTMP != null) scoreTextTMP.text = $"Score: {finalScore}";

        if (survivalTimeText != null) survivalTimeText.text = timeStr;
        if (survivalTimeTextTMP != null) survivalTimeTextTMP.text = timeStr;

        if (enemiesKilledText != null) enemiesKilledText.text = $"Enemies Killed: {killedCount}";
        if (enemiesKilledTextTMP != null) enemiesKilledTextTMP.text = $"Enemies Killed: {killedCount}";

        // Try to find buttons programmatically if not assigned
        if (restartButton == null) restartButton = GameObject.Find("RestartButton")?.GetComponent<Button>();
        if (mainMenuButton == null) mainMenuButton = GameObject.Find("MainMenuButton")?.GetComponent<Button>();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }

        // Ensure timeScale is normal so buttons interact
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetSession();
            GameManager.Instance.SetState(GameManager.State.Playing);
        }
        SceneManager.LoadScene("Level1");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
