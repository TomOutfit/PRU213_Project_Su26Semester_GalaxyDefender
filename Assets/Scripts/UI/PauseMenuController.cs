using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Quản lý Pause overlay (Canvas_Pause).
/// - Canvas_Pause mặc định ẩn khi bắt đầu màn chơi.
/// - PauseGame()     → gọi từ nút Pause trên HUD.
/// - OnResumeClick() → nút Continue trong Pause menu.
/// - ESC key cũng toggle pause.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Stats Text (TMP)")]
    public TMPro.TMP_Text scoreTextTMP;
    public TMPro.TMP_Text survivalTimeTextTMP;
    public TMPro.TMP_Text enemiesKilledTextTMP;

    [Header("UI Panel")]
    [Tooltip("Kéo Canvas_Pause vào đây trong Inspector")]
    public GameObject pausePanel;

    [Header("Buttons")]
    [Tooltip("Nút Pause trên HUD")]
    public Button pauseButton;
    [Tooltip("Nút Continue bên trong Pause menu")]
    public Button resumeButton;
    [Tooltip("Nút Restart bên trong Pause menu")]
    public Button restartButton;
    [Tooltip("Nút Menu bên trong Pause menu")]
    public Button mainMenuButton;

    private bool _subscribed = false;

    // ─────────────────────────────────────────
    private void Awake()
    {
        // Ẩn panel ngay trước khi bất cứ thứ gì render
        HidePausePanel();
    }

    private void OnEnable()
    {
        TrySubscribeGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeGameManager();
    }

    private void Start()
    {
        // Fallback tìm panel nếu chưa gán — DÙNG FindObjectsOfType vì Find() bỏ qua inactive
        if (pausePanel == null)
        {
            // Tìm trong tất cả canvas kể cả inactive
            Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (var c in allCanvases)
            {
                if (c.gameObject.scene == gameObject.scene &&
                    (c.gameObject.name == "Canvas_Pause" || c.gameObject.name == "PausePanel"))
                {
                    pausePanel = c.gameObject;
                    break;
                }
            }
        }

        // Tìm buttons nếu chưa gán
        if (pauseButton    == null) pauseButton    = FindActiveButton("PauseButton");
        if (resumeButton   == null) resumeButton   = FindButtonInPanel("BtnContinue");
        if (restartButton  == null) restartButton  = FindButtonInPanel("BtnRestart");
        if (mainMenuButton == null) mainMenuButton = FindButtonInPanel("BtnMenu");

        // Wire listeners
        WireButton(pauseButton,    PauseGame);
        WireButton(resumeButton,   OnResumeClick);
        WireButton(restartButton,  OnRestartClick);
        WireButton(mainMenuButton, OnMainMenuClick);

        // Đảm bảo panel ẩn
        HidePausePanel();

        // Subscribe GameManager (nếu chưa trong OnEnable)
        if (!_subscribed)
            TrySubscribeGameManager();

        // Nếu GameManager chưa sẵn sàng, dùng coroutine chờ
        if (!_subscribed)
            StartCoroutine(WaitAndSubscribe());

        // Debug log
        Debug.Log($"[PauseMenu] pausePanel={pausePanel?.name ?? "NULL"} | " +
                  $"pauseBtn={pauseButton?.name ?? "NULL"} | " +
                  $"resumeBtn={resumeButton?.name ?? "NULL"} | " +
                  $"GM={GameManager.Instance?.name ?? "NULL"} | subscribed={_subscribed}");
    }

    // Retry coroutine — đợi GameManager khởi tạo xong
    private IEnumerator WaitAndSubscribe()
    {
        float timeout = 5f;
        while (!_subscribed && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
            TrySubscribeGameManager();
        }
        if (!_subscribed)
            Debug.LogWarning("[PauseMenu] ⚠️ Could not subscribe to GameManager after 5s!");
        else
            Debug.Log("[PauseMenu] ✅ Subscribed to GameManager via coroutine.");
    }

    private void Update()
    {
        // ESC toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState == GameManager.State.Playing)
                PauseGame();
            else if (GameManager.Instance.CurrentState == GameManager.State.Paused)
                OnResumeClick();
        }

        // Safety: nếu chưa subscribe mà GameManager đã có, subscribe ngay
        if (!_subscribed)
            TrySubscribeGameManager();
    }

    // ─────────────────────────────────────────
    //  GAMEMANAGER SUBSCRIPTION
    // ─────────────────────────────────────────
    private void TrySubscribeGameManager()
    {
        if (_subscribed) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStateChanged.AddListener(OnStateChanged);
        _subscribed = true;

        // Sync trạng thái hiện tại ngay khi subscribe
        OnStateChanged(GameManager.Instance.CurrentState);
    }

    private void UnsubscribeGameManager()
    {
        if (!_subscribed) return;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged.RemoveListener(OnStateChanged);
        _subscribed = false;
    }

    // ─────────────────────────────────────────
    //  STATE HANDLER
    // ─────────────────────────────────────────
    private void OnStateChanged(GameManager.State state)
    {
        bool isPaused = (state == GameManager.State.Paused);

        if (isPaused)
            ShowPausePanel();
        else
            HidePausePanel();

        if (isPaused)
            UpdateStatsDisplay();
    }

    // ─────────────────────────────────────────
    //  PANEL SHOW / HIDE
    // ─────────────────────────────────────────
    private void ShowPausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Debug.Log("[PauseMenu] Canvas_Pause → SHOW");
        }
        else
        {
            Debug.LogWarning("[PauseMenu] ⚠️ pausePanel is NULL — cannot show!");
        }
    }

    private void HidePausePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // ─────────────────────────────────────────
    //  STATS DISPLAY
    // ─────────────────────────────────────────
    private void UpdateStatsDisplay()
    {
        int   score  = ScoreManager.Instance != null ? ScoreManager.Instance.currentScore       : 0;
        float time   = GameManager.Instance  != null ? GameManager.Instance.survivalTime        : 0f;
        int   killed = GameManager.Instance  != null ? GameManager.Instance.totalEnemiesKilled  : 0;

        int min = Mathf.FloorToInt(time / 60f);
        int sec = Mathf.FloorToInt(time % 60f);

        if (scoreTextTMP         != null) scoreTextTMP.text         = $"SCORE: {score:N0}";
        if (enemiesKilledTextTMP != null) enemiesKilledTextTMP.text = $"ENEMIES DEFEATED: {killed}";
        if (survivalTimeTextTMP  != null) survivalTimeTextTMP.text  = $"SURVIVAL TIME: {min}:{sec:D2}";
    }

    // ─────────────────────────────────────────
    //  BUTTON HANDLERS
    // ─────────────────────────────────────────

    /// <summary>Gắn vào nút Pause trên HUD.</summary>
    public void PauseGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PauseMenu] PauseGame() — GameManager.Instance is NULL");
            return;
        }
        if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

        GameManager.Instance.SetState(GameManager.State.Paused);
    }

    /// <summary>Nút CONTINUE.</summary>
    public void OnResumeClick()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.State.Playing);
    }

    /// <summary>Nút RESTART.</summary>
    public void OnRestartClick()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetSession();
            GameManager.Instance.SetState(GameManager.State.Playing);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Nút MENU.</summary>
    public void OnMainMenuClick()
    {
        Time.timeScale = 1f;

        // Save current level, score AND wave index để Continue có thể khôi phục đúng wave
        if (SaveManager.Instance != null)
        {
            int currentLevelIndex = SceneManager.GetActiveScene().buildIndex;
            int currentScore      = ScoreManager.Instance != null ? ScoreManager.Instance.currentScore : 0;
            // GetCurrentWaveIndex() trả về 0-based index; -1 nếu WaveManager chưa có → clamp về 0
            int currentWaveIndex  = WaveManager.Instance != null
                                        ? Mathf.Max(0, WaveManager.Instance.GetCurrentWaveIndex())
                                        : 0;

            SaveManager.Instance.SaveGame(currentLevelIndex, currentScore, currentWaveIndex);
            Debug.Log($"[PauseMenu] Saved → Level={currentLevelIndex} Score={currentScore} Wave={currentWaveIndex}");
        }

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────
    private static void WireButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private Button FindActiveButton(string name)
    {
        var go = GameObject.Find(name);
        return go?.GetComponent<Button>();
    }

    private Button FindButtonInPanel(string name)
    {
        if (pausePanel == null) return null;
        var t = pausePanel.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in t)
            if (child.name == name)
                return child.GetComponent<Button>();
        return null;
    }
}
