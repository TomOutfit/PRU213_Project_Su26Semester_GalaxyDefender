using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { Playing, Paused, GameOver, Victory }
    public State CurrentState { get; private set; }

    public UnityEvent<State> OnStateChanged;
    public UnityEvent OnGameOver;

    [HideInInspector]
    public float survivalTime;

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

    private void Start()
    {
        SetState(State.Playing);
    }

    private void Update()
    {
        if (CurrentState == State.Playing)
        {
            survivalTime += Time.deltaTime;
        }
    }

    public void SetState(State newState)
    {
        State prev = CurrentState;
        CurrentState = newState;

        switch (newState)
        {
            case State.Paused:
                Time.timeScale = 0f;
                break;
            case State.Playing:
                Time.timeScale = 1f;
                break;
            case State.GameOver:
                StartCoroutine(GameOverSequence());
                break;
            case State.Victory:
                Time.timeScale = 1f;
                break;
        }

        OnStateChanged?.Invoke(newState);
    }

    private System.Collections.IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }

    public void TriggerGameOver()
    {
        if (CurrentState != State.GameOver)
        {
            SetState(State.GameOver);
            OnGameOver?.Invoke();
        }
    }
}
