using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int currentScore { get; internal set; }
    public int comboCount { get; private set; }
    public bool powerUpMultiplierActive { get; private set; }

    public UnityEvent<int> OnScoreChanged;
    public UnityEvent OnPlayerDamagedEvent;
    public UnityEvent OnEnemyKilledEvent;

    private Coroutine multiplierTimerRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private PlayerHealth playerRef;

    private void Start()
    {
        playerRef = FindAnyObjectByType<PlayerHealth>();
        if (playerRef != null)
        {
            playerRef.OnDamageTaken.AddListener(OnPlayerDamaged);
        }
    }

    private void OnDestroy()
    {
        if (playerRef != null)
        {
            playerRef.OnDamageTaken.RemoveListener(OnPlayerDamaged);
        }
    }

    public void AddScore(int baseScore)
    {
        int multiplier = GetMultiplier();
        currentScore += baseScore * multiplier;
        OnScoreChanged?.Invoke(currentScore);
    }

    public int GetMultiplier()
    {
        if (powerUpMultiplierActive) return 2;
        if (comboCount >= 10) return 3;
        if (comboCount >= 5) return 2;
        return 1;
    }

    public void OnPlayerDamaged()
    {
        comboCount = 0;
        OnPlayerDamagedEvent?.Invoke();
    }

    public void OnEnemyKilled()
    {
        comboCount++;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.totalEnemiesKilled++;
        }
        OnEnemyKilledEvent?.Invoke();
    }

    public void ActivateScoreMultiplier(float duration = 10f)
    {
        powerUpMultiplierActive = true;

        if (multiplierTimerRoutine != null)
            StopCoroutine(multiplierTimerRoutine);

        multiplierTimerRoutine = StartCoroutine(MultiplierTimer(duration));
    }

    private IEnumerator MultiplierTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        powerUpMultiplierActive = false;
    }

    public void ResetScore()
    {
        currentScore = 0;
        comboCount = 0;
        powerUpMultiplierActive = false;
        OnScoreChanged?.Invoke(currentScore);
    }
}
