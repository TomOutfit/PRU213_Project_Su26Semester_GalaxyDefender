using UnityEngine;
using System.Collections;

public class PowerUpScore : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ActivateScoreMultiplier(50f);
    }

    protected override int ScoreValue => 200;
    protected override string SFXKey => "sfx_powerup_score";
}
