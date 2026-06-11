using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject pausePanel; // Panel to show/hide

    private void Start()
    {
        // If pausePanel is not assigned, we can default to this gameobject
        if (pausePanel == null)
            pausePanel = gameObject;

        // Sync initial state
        UpdatePanelState();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged.AddListener(OnStateChanged);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.State.Playing)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager.Instance.SetState(GameManager.State.Paused);
            }
        }
    }

    private void OnStateChanged(GameManager.State state)
    {
        UpdatePanelState();
    }

    private void UpdatePanelState()
    {
        if (pausePanel != null && GameManager.Instance != null)
        {
            pausePanel.SetActive(GameManager.Instance.CurrentState == GameManager.State.Paused);
        }
    }

    // Button click handlers
    public void OnResumeClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.State.Playing);
        }
    }

    public void OnRestartClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
