using UnityEngine;
using System.Collections;

public class PowerUpItemMagnet : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.StartCoroutine(MagnetRoutine());
    }

    private IEnumerator MagnetRoutine()
    {
        PowerUp.MagnetRadiusMultiplier = 4f;
        yield return new WaitForSeconds(10f);
        PowerUp.MagnetRadiusMultiplier = 1f;
    }

    protected override int ScoreValue => 60;
    protected override string SFXKey => "sfx_powerup_shield";
}
