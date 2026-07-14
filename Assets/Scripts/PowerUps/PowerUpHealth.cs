using UnityEngine;

public class PowerUpHealth : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddHealth(ph.maxHP - ph.currentHP);
    }

    protected override int ScoreValue => 10000;
    protected override string SFXKey => "sfx_powerup_health";
}
