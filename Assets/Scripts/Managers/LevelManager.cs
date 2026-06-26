using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Scene Flow (in build order)")]
    [Tooltip("Leave empty for automatic flow. Override specific indices here if needed.")]
    public string[] sceneNames = { "Level1", "Level2", "Level3_Boss", "Victory" };

    private int GetSceneIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains(sceneName))
                return i;
        }
        return -1;
    }

    private int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }

    private string GetNextSceneName()
    {
        int currentIdx = GetCurrentSceneIndex();

        for (int i = 0; i < sceneNames.Length - 1; i++)
        {
            int targetIdx = GetSceneIndex(sceneNames[i]);
            if (targetIdx == currentIdx && i + 1 < sceneNames.Length)
                return sceneNames[i + 1];
        }

        // Fallback: advance by build index
        return sceneNames[Mathf.Min(currentIdx + 1, sceneNames.Length - 1)];
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Restore the accumulated score into the new scene's ScoreManager
        if (ScoreManager.Instance != null && _pendingScore > 0)
        {
            ScoreManager.Instance.currentScore = _pendingScore;
            ScoreManager.Instance.OnScoreChanged?.Invoke(_pendingScore);
            _pendingScore = 0; // consumed
        }

        if (scene.name == "Victory")
        {
            Victory();
        }
    }

    // Score to carry over to the next scene
    private int _pendingScore = 0;

    public void LevelComplete()
    {
        StartCoroutine(LevelCompleteSequence());
    }

    private IEnumerator LevelCompleteSequence()
    {
        // Show "LEVEL COMPLETE!" banner via HUD
        HUDController hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            hud.DisplayMessage("LEVEL COMPLETE!", 2f);
        }

        yield return new WaitForSeconds(2f);

        string nextScene = GetNextSceneName();

        // Save accumulated score to carry forward AND save to PlayerPrefs
        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.currentScore : 0;
        _pendingScore = currentScore;

        int nextIndex = GetSceneIndex(nextScene);
        SaveManager.Instance?.SaveGame(nextIndex, currentScore);

        // Use cinematic fade transition
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(nextScene);
        }
        else
        {
            SceneManager.LoadScene(nextScene);
        }
    }

    public void Victory()
    {
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        HUDController hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            hud.DisplayMessage("VICTORY!", 3f);
        }

        yield return new WaitForSeconds(3f);

        SaveManager.Instance?.SaveHighScore(ScoreManager.Instance?.currentScore ?? 0);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
