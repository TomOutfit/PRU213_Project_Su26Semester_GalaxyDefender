using UnityEngine;

public class PowerUpHealth : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddHealth(300);
    }

    protected override string SFXKey => "sfx_powerup_health";
}
