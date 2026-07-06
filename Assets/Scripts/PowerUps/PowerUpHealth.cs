using UnityEngine;

public class PowerUpHealth : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddHealth(500);
    }

    protected override int ScoreValue => 80;
    protected override string SFXKey => "sfx_powerup_health";
}
