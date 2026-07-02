using UnityEngine;

public class PowerUpMegaBomb : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        // Clear all active non-boss enemies in the scene
        EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.GetComponent<BossController>() == null)
            {
                enemy.TakeDamage(9999);
            }
        }
    }

    protected override int ScoreValue => 150;
    protected override string SFXKey => "sfx_explosion_large";
}
