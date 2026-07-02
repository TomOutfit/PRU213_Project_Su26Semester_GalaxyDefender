using UnityEngine;

public class PowerUpShield : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddShield(250);
    }

    protected override string SFXKey => "sfx_powerup_shield";
}
