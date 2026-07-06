using UnityEngine;

public class PowerUpShield : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddShield(5000);
    }

    protected override int ScoreValue => 100;
    protected override string SFXKey => "sfx_powerup_shield";
}
