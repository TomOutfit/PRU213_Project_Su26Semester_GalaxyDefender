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
    }

    public void LevelComplete()
    {
        StartCoroutine(LevelCompleteSequence());
    }

    private IEnumerator LevelCompleteSequence()
    {
        yield return new WaitForSeconds(2f);

        string nextScene = GetNextSceneName();
        SceneManager.LoadScene(nextScene);
    }

    public void Victory()
    {
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        yield return new WaitForSeconds(3f);

        SaveManager.Instance?.SaveHighScore(ScoreManager.Instance?.currentScore ?? 0);
        SceneManager.LoadScene("MainMenu");
    }
}
