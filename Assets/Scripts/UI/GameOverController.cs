using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameOverController : MonoBehaviour
{
    [Header("Stats Text (TMP)")]
    public TMP_Text scoreTextTMP;
    public TMP_Text survivalTimeTextTMP;
    public TMP_Text enemiesKilledTextTMP;

    [Header("Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Title Flicker")]
    public TMP_Text titleText;
    public float flickerInterval = 0.08f;
    public int flickerCount = 6;

    private void Awake()
    {
        // Auto-find by name if not assigned in Inspector
        if (restartButton == null)
            restartButton = GameObject.Find("RestartButton")?.GetComponent<Button>();
        if (mainMenuButton == null)
            mainMenuButton = GameObject.Find("MainMenuButton")?.GetComponent<Button>();
        if (titleText == null)
            titleText = GameObject.Find("TitleText")?.GetComponent<TMP_Text>();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(Restart);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(MainMenu);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.State.GameOver)
        {
            GameManager.Instance.SetState(GameManager.State.GameOver);
        }

        int finalScore   = ScoreManager.Instance != null ? ScoreManager.Instance.currentScore : 0;
        float timeVal    = GameManager.Instance   != null ? GameManager.Instance.survivalTime  : 0f;
        int killedCount  = GameManager.Instance   != null ? GameManager.Instance.totalEnemiesKilled : 0;

        int minutes = Mathf.FloorToInt(timeVal / 60f);
        int seconds = Mathf.FloorToInt(timeVal % 60f);
        string timeStr = $"{minutes}:{seconds:D2}";

        if (scoreTextTMP        != null) scoreTextTMP.text        = $"FINAL SCORE: {finalScore:N0}";
        if (enemiesKilledTextTMP!= null) enemiesKilledTextTMP.text= $"ENEMIES DEFEATED: {killedCount}";
        if (survivalTimeTextTMP != null) survivalTimeTextTMP.text = $"SURVIVAL TIME: {timeStr}";

        // Save high score
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveHighScore(finalScore);

        // Play BGM
        AudioManager.Instance?.PlayBGM("bgm_gameover");

        // Animate title
        if (titleText != null)
            StartCoroutine(FlickerTitle());
    }

    private IEnumerator FlickerTitle()
    {
        yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < flickerCount; i++)
        {
            titleText.enabled = false;
            yield return new WaitForSeconds(flickerInterval);
            titleText.enabled = true;
            yield return new WaitForSeconds(flickerInterval * 1.5f);
        }
    }

    public void Restart()
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

    public void MainMenu()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }
}
