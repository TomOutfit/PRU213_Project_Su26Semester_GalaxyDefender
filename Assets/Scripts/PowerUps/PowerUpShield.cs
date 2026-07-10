using UnityEngine;

public class PowerUpShield : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddShield(25000);
    }

    protected override int ScoreValue => 10000;
    protected override string SFXKey => "sfx_powerup_shield";
}
