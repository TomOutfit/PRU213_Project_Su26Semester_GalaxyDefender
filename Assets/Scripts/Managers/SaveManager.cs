using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                }
            }
            return _instance;
        }
    }

    private const string KEY_LAST_LEVEL = "LastLevel";
    private const string KEY_LAST_SCORE = "LastScore";
    private const int HIGH_SCORE_COUNT = 5;
    private const string KEY_HIGH_SCORE_PREFIX = "HighScore_";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame(int levelBuildIndex, int score)
    {
        PlayerPrefs.SetInt(KEY_LAST_LEVEL, levelBuildIndex);
        PlayerPrefs.SetInt(KEY_LAST_SCORE, score);
        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        int savedLevel = PlayerPrefs.GetInt(KEY_LAST_LEVEL, 1);
        int savedScore = PlayerPrefs.GetInt(KEY_LAST_SCORE, 0);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.currentScore = savedScore;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.State.Playing);

        UnityEngine.SceneManagement.SceneManager.LoadScene(savedLevel);
    }

    public void SaveHighScore(int score)
    {
        if (score <= 0) return;

        List<int> scores = new List<int>();
        for (int i = 0; i < HIGH_SCORE_COUNT; i++)
        {
            int saved = PlayerPrefs.GetInt(KEY_HIGH_SCORE_PREFIX + i, 0);
            scores.Add(saved);
        }

        scores.Add(score);
        scores.Sort((a, b) => b.CompareTo(a)); // descending

        for (int i = 0; i < HIGH_SCORE_COUNT; i++)
        {
            PlayerPrefs.SetInt(KEY_HIGH_SCORE_PREFIX + i, scores[i]);
        }
        PlayerPrefs.Save();
    }

    public int[] GetHighScores()
    {
        int[] scores = new int[HIGH_SCORE_COUNT];
        for (int i = 0; i < HIGH_SCORE_COUNT; i++)
        {
            scores[i] = PlayerPrefs.GetInt(KEY_HIGH_SCORE_PREFIX + i, 0);
        }
        return scores;
    }

    public void ClearHighScores()
    {
        for (int i = 0; i < HIGH_SCORE_COUNT; i++)
        {
            PlayerPrefs.DeleteKey(KEY_HIGH_SCORE_PREFIX + i);
        }
        PlayerPrefs.Save();
    }
}
