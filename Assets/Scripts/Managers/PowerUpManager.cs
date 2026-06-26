using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Power-Up Prefabs")]
    public GameObject[] powerUpPrefabs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Drop(Vector3 position)
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;
        
        // Increased drop rate to 50% for better visibility
        if (Random.value >= 0.5f) return;

        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
        GameObject powerUp = Instantiate(prefab, position, Quaternion.identity);
        
        Debug.Log($"[PowerUpManager] Dropped {prefab.name} at {position}");
    }
}

