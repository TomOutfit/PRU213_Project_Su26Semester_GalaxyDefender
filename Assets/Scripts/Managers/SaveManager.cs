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
                _instance = FindAnyObjectByType<SaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                }
            }
            return _instance;
        }
    }

    private const string KEY_LAST_LEVEL    = "LastLevel";
    private const string KEY_LAST_SCORE    = "LastScore";
    private const string KEY_LAST_WAVE     = "LastWave";      // 0-based wave index
    private const string KEY_VICTORY       = "GameCleared";  // 1 = all levels cleared
    private const int    HIGH_SCORE_COUNT  = 5;
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

    /// <summary>
    /// Lưu level đang chơi + điểm hiện tại + wave index (0-based).
    /// Chỉ gọi khi thoát level thực sự (Level1/2/3), KHÔNG gọi khi vào Victory/GameOver.
    /// </summary>
    /// <param name="waveIndex">0-based index của wave đang diễn ra. Mặc định 0 = Wave 1.</param>
    public void SaveGame(int levelBuildIndex, int score, int waveIndex = 0)
    {
        int roundedScore = Mathf.RoundToInt((float)score / 1000f) * 1000;
        PlayerPrefs.SetInt(KEY_LAST_LEVEL, levelBuildIndex);
        PlayerPrefs.SetInt(KEY_LAST_SCORE, roundedScore);
        PlayerPrefs.SetInt(KEY_LAST_WAVE,  Mathf.Max(0, waveIndex));
        PlayerPrefs.Save();
    }

    /// <summary>Trả về wave index (0-based) đã lưu lần cuối. Mặc định 0 nếu chưa có save.</summary>
    public int GetLastWave() => PlayerPrefs.GetInt(KEY_LAST_WAVE, 0);

    /// <summary>
    /// Gọi khi người chơi chiến thắng toàn bộ game.
    /// Đánh dấu trạng thái cleared và ghi lại điểm cuối.
    /// LastLevel sẽ KHÔNG thay đổi — giữ nguyên level cuối đã chơi.
    /// </summary>
    public void SaveVictory(int score)
    {
        int roundedScore = Mathf.RoundToInt((float)score / 1000f) * 1000;
        PlayerPrefs.SetInt(KEY_VICTORY, 1);
        PlayerPrefs.SetInt(KEY_LAST_SCORE, roundedScore);
        PlayerPrefs.Save();
    }

    /// <summary>Trả về true nếu người chơi đã hoàn thành toàn bộ game.</summary>
    public bool IsVictoryCleared() => PlayerPrefs.GetInt(KEY_VICTORY, 0) == 1;

    public void LoadGame()
    {
        int savedLevel = PlayerPrefs.GetInt(KEY_LAST_LEVEL, 1);
        int savedScore = PlayerPrefs.GetInt(KEY_LAST_SCORE, 0);
        int savedWave  = PlayerPrefs.GetInt(KEY_LAST_WAVE,  0);
        savedScore = Mathf.RoundToInt((float)savedScore / 1000f) * 1000;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.currentScore = savedScore;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.State.Playing);

        // Báo cho WaveManager biết phải bắt đầu từ wave nào khi scene load xong
        WaveManager.pendingResumeWave = savedWave;

        UnityEngine.SceneManagement.SceneManager.LoadScene(savedLevel);
    }

    public void SaveHighScore(int score)
    {
        if (score <= 0) return;

        int roundedScore = Mathf.RoundToInt((float)score / 1000f) * 1000;

        List<int> scores = new List<int>();
        for (int i = 0; i < HIGH_SCORE_COUNT; i++)
        {
            int saved = PlayerPrefs.GetInt(KEY_HIGH_SCORE_PREFIX + i, 0);
            saved = Mathf.RoundToInt((float)saved / 1000f) * 1000;
            scores.Add(saved);
        }

        scores.Add(roundedScore);
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
            int saved = PlayerPrefs.GetInt(KEY_HIGH_SCORE_PREFIX + i, 0);
            scores[i] = Mathf.RoundToInt((float)saved / 1000f) * 1000;
        }
        return scores;
    }

    public void ClearHighScores()
    {
        for (int i = 0; i < HIGH_SCORE_COUNT; i++)
        {
            PlayerPrefs.DeleteKey(KEY_HIGH_SCORE_PREFIX + i);
        }
        // Xoá cả trạng thái victory, last level và wave khi reset toàn bộ
        PlayerPrefs.DeleteKey(KEY_VICTORY);
        PlayerPrefs.DeleteKey(KEY_LAST_LEVEL);
        PlayerPrefs.DeleteKey(KEY_LAST_SCORE);
        PlayerPrefs.DeleteKey(KEY_LAST_WAVE);
        WaveManager.pendingResumeWave = -1; // xoá cả flag runtime
        PlayerPrefs.Save();
    }
}
