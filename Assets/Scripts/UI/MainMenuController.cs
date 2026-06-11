using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    }

    public void OnStartClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetSession();
            GameManager.Instance.SetState(GameManager.State.Playing);
        }
        SceneManager.LoadScene("Level1");
    }

    public void OnLoadClick()
    {
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
