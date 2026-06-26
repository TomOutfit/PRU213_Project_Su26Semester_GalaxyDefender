using UnityEngine;

/// <summary>
/// Attach to any Tilemap_Hazard GameObject (TilemapCollider2D set to isTrigger).
/// Deals 10 damage per second to the player while they stand on the hazard tile.
/// </summary>
public class TilemapHazard : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        PlayerHealth ph = collision.GetComponent<PlayerHealth>();
        ph?.TakeDamage(Mathf.RoundToInt(10f * Time.deltaTime));
    }
}
