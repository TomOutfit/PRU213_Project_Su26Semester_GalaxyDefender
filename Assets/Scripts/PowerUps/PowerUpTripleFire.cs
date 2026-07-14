using UnityEngine;
using System.Collections;

public class PowerUpTripleFire : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        PlayerController pc = ph.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.UpgradeWeapon();
        }
    }

    protected override int ScoreValue => 10000;
    protected override string SFXKey => "sfx_powerup_score";
}
