using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject highScorePanel;

    [Header("Buttons")]
    public Button startButton;
    public Button loadButton;
    public Button optionsButton;
    public Button highScoreButton;
    public Button exitButton;

    [Header("High Score Labels (5 TMP labels in order)")]
    public TMP_Text[] highScoreLabels = new TMP_Text[5];

    [Header("Level Indicator")]
    public TMP_Text levelIndicatorText;

    private void Awake()
    {
        // Auto-resolve references if not set
        if (levelIndicatorText == null) levelIndicatorText = GameObject.Find("LevelIndicator")?.GetComponent<TMP_Text>();
        if (optionsPanel == null) optionsPanel = GameObject.Find("OptionsPanel");
        if (highScorePanel == null) highScorePanel = GameObject.Find("HighScorePanel");

        for (int i = 0; i < 5; i++)
        {
            if (highScoreLabels[i] == null)
            {
                highScoreLabels[i] = GameObject.Find($"highScore{i + 1}")?.GetComponent<TMP_Text>();
            }
        }

        // Populate high score labels
        int[] scores = SaveManager.Instance != null ? SaveManager.Instance.GetHighScores() : new int[5];
        if (SaveManager.Instance == null)
        {
            for (int i = 0; i < 5; i++) scores[i] = PlayerPrefs.GetInt("HighScore_" + i, 0);
        }

        for (int i = 0; i < highScoreLabels.Length; i++)
        {
            if (highScoreLabels[i] != null)
                highScoreLabels[i].text = $"{i + 1}. {scores[i]:N0}";
        }

        // Show level indicator
        if (levelIndicatorText != null)
        {
            if (PlayerPrefs.HasKey("LastLevel"))
            {
                int lastLevel = PlayerPrefs.GetInt("LastLevel", 1);
                levelIndicatorText.text = $"Last played: Level {lastLevel}";
                levelIndicatorText.gameObject.SetActive(true);
            }
            else
            {
                levelIndicatorText.gameObject.SetActive(false);
            }
        }

        // Initial panel state
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (highScorePanel != null) highScorePanel.SetActive(false);
    }

    private void Start()
    {
        if (startButton == null) startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (loadButton == null) loadButton = GameObject.Find("LoadButton")?.GetComponent<Button>();
        if (optionsButton == null) optionsButton = GameObject.Find("OptionsButton")?.GetComponent<Button>();
        if (highScoreButton == null) highScoreButton = GameObject.Find("HighScoreButton")?.GetComponent<Button>();
        if (exitButton == null) exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();

        if (startButton != null) startButton.onClick.AddListener(OnStartClick);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadClick);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClick);
        if (highScoreButton != null) highScoreButton.onClick.AddListener(OnHighScoreClick);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClick);

        // Attach and run main menu visual effects
        if (GetComponent<MainMenuEffectsInitializer>() == null)
        {
            gameObject.AddComponent<MainMenuEffectsInitializer>();
        }

        // Ensure SceneTransitionManager exists so the menu fade-in plays
        if (SceneTransitionManager.Instance == null)
        {
            GameObject tm = new GameObject("[SceneTransitionManager]");
            tm.AddComponent<SceneTransitionManager>();
        }
    }

    public void OnStartClick()
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

    public void OnLoadClick()
    {
        // LoadGame calls SceneManager.LoadScene internally; wrap it with transition
        int savedLevel = UnityEngine.PlayerPrefs.GetInt("LastLevel", 1);
        int savedScore = UnityEngine.PlayerPrefs.GetInt("LastScore", 0);
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.currentScore = savedScore;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(savedLevel);
        else
            SaveManager.Instance?.LoadGame();
    }

    public void OnOptionsClick()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(!optionsPanel.activeSelf);
        }
    }

    public void OnHighScoreClick()
    {
        if (highScorePanel != null)
        {
            highScorePanel.SetActive(!highScorePanel.activeSelf);
        }
    }

    public void OnExitClick()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
