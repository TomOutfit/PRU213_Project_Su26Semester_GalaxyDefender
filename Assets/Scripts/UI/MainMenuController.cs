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

        if (highScorePanel != null)
        {
            TMP_Text[] allTexts = highScorePanel.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < 5; i++)
            {
                if (highScoreLabels[i] == null)
                {
                    string targetName = $"highScore{i + 1}";
                    foreach (var txt in allTexts)
                    {
                        if (txt.gameObject.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            highScoreLabels[i] = txt;
                            break;
                        }
                    }
                }
            }
        }

        // Populate high score labels dynamically
        RefreshMenuScores();

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
        // Find buttons directly
        if (startButton == null) startButton = FindButton("MenuButtons/StartButton");
        if (loadButton == null) loadButton = FindButton("MenuButtons/LoadButton");
        if (optionsButton == null) optionsButton = FindButton("MenuButtons/OptionsButton");
        if (highScoreButton == null) highScoreButton = FindButton("MenuButtons/HighScoreButton");
        if (exitButton == null) exitButton = FindButton("MenuButtons/ExitButton");

        if (startButton != null) startButton.onClick.RemoveListener(OnStartClick);
        if (loadButton != null) loadButton.onClick.RemoveListener(OnLoadClick);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsClick);
        if (highScoreButton != null) highScoreButton.onClick.RemoveListener(OnHighScoreClick);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClick);

        if (startButton != null) startButton.onClick.AddListener(OnStartClick);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadClick);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClick);
        if (highScoreButton != null) highScoreButton.onClick.AddListener(OnHighScoreClick);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClick);

        // Ensure SceneTransitionManager exists so the menu fade-in plays
        if (SceneTransitionManager.Instance == null)
        {
            GameObject tm = new GameObject("[SceneTransitionManager]");
            tm.AddComponent<SceneTransitionManager>();
        }

        // Fade in the overlay from black (for when entering MainMenu via SceneTransition)
        StartCoroutine(FadeInMenuOverlay());
    }

    private System.Collections.IEnumerator FadeInMenuOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        // Find FadeOverlay → CanvasGroup
        Transform fadeT = canvas.transform.Find("FadeOverlay");
        CanvasGroup cg = fadeT != null ? fadeT.GetComponent<CanvasGroup>() : null;

        // Also check SceneTransitionManager's own overlay
        if (SceneTransitionManager.Instance != null)
        {
            // The STM creates its own overlay; just wait a moment then return
            yield break;
        }

        if (cg != null)
        {
            // Wait one frame for the scene to fully settle
            yield return null;

            float duration = 0.7f;
            float elapsed = 0f;
            float startAlpha = cg.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
        }
    }

    private static Button FindButton(string path)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;
        Transform t = canvas.transform.Find(path);
        return t != null ? t.GetComponent<Button>() : null;
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
            bool nextState = !highScorePanel.activeSelf;
            highScorePanel.SetActive(nextState);
            if (nextState)
            {
                RefreshMenuScores();
            }
        }
    }

    private void RefreshMenuScores()
    {
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
