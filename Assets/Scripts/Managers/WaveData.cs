using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Galaxy Defender/Wave Data")]
public class WaveData : ScriptableObject
{
    public GameObject enemyPrefab;
    public int enemyCount = 4;
    public float[] spawnPositionsX; // screen-width percentages (0–1)
    public float speedMultiplier = 1f;
    public float spawnDelay = 0.3f;
}
