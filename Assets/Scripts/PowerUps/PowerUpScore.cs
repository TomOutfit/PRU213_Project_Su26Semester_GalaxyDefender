using UnityEngine;
using System.Collections;

public class PowerUpScore : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ActivateScoreMultiplier(10f);
    }

    protected override string SFXKey => "sfx_powerup_score";
}
