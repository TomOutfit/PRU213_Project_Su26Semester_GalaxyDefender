using UnityEngine;

public class PowerUpHealth : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddHealth(50000);
    }

    protected override int ScoreValue => 10000;
    protected override string SFXKey => "sfx_powerup_health";
}
