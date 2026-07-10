using UnityEngine;

public class PowerUpMegaBomb : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        // Clear all active non-boss enemies in the scene, and deal massive damage to the Boss
        EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                if (enemy.GetComponent<BossController>() != null)
                {
                    enemy.TakeDamage(100000); // 100k damage to Boss (1/3 of its 300k HP)
                }
                else
                {
                    enemy.TakeDamage(999999); // 999k damage to normal enemies to ensure instant kill
                }
            }
        }
    }

    protected override int ScoreValue => 10000;
    protected override string SFXKey => "sfx_explosion_large";
}
