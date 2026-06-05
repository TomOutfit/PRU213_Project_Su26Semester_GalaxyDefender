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
        SetupCollisionMatrix();
    }

    private void SetupCollisionMatrix()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int bossLayer = LayerMask.NameToLayer("Boss");
        int playerBulletLayer = LayerMask.NameToLayer("PlayerBullet");
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        int powerUpLayer = LayerMask.NameToLayer("PowerUp");

        if (playerLayer != -1 && enemyLayer != -1 && bossLayer != -1 && 
            playerBulletLayer != -1 && enemyBulletLayer != -1 && powerUpLayer != -1)
        {
            // Clear default collisions first for these layers
            for (int i = 0; i < 32; i++)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, i, true);
                Physics2D.IgnoreLayerCollision(enemyLayer, i, true);
                Physics2D.IgnoreLayerCollision(bossLayer, i, true);
                Physics2D.IgnoreLayerCollision(playerBulletLayer, i, true);
                Physics2D.IgnoreLayerCollision(enemyBulletLayer, i, true);
                Physics2D.IgnoreLayerCollision(powerUpLayer, i, true);
            }

            // Player collisions: Enemy, Boss, EnemyBullet, PowerUp
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
            Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false);
            Physics2D.IgnoreLayerCollision(playerLayer, enemyBulletLayer, false);
            Physics2D.IgnoreLayerCollision(playerLayer, powerUpLayer, false);

            // Enemy collisions: Player, PlayerBullet
            Physics2D.IgnoreLayerCollision(enemyLayer, playerLayer, false);
            Physics2D.IgnoreLayerCollision(enemyLayer, playerBulletLayer, false);

            // Boss collisions: Player, PlayerBullet
            Physics2D.IgnoreLayerCollision(bossLayer, playerLayer, false);
            Physics2D.IgnoreLayerCollision(bossLayer, playerBulletLayer, false);

            // PlayerBullet collisions: Enemy, Boss
            Physics2D.IgnoreLayerCollision(playerBulletLayer, enemyLayer, false);
            Physics2D.IgnoreLayerCollision(playerBulletLayer, bossLayer, false);

            // EnemyBullet collisions: Player
            Physics2D.IgnoreLayerCollision(enemyBulletLayer, playerLayer, false);

            // PowerUp collisions: Player
            Physics2D.IgnoreLayerCollision(powerUpLayer, playerLayer, false);
        }
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
