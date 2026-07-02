using UnityEngine;
using System.Collections;

public class PowerUpSpeedBolt : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        PlayerController pc = ph.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.StartCoroutine(SpeedRoutine(pc));
        }
    }

    private IEnumerator SpeedRoutine(PlayerController pc)
    {
        float originalSpeed = pc.moveSpeed;
        pc.moveSpeed = originalSpeed * 1.5f; // +50% speed boost
        yield return new WaitForSeconds(10f);
        if (pc != null)
        {
            pc.moveSpeed = originalSpeed;
        }
    }

    protected override int ScoreValue => 50;
    protected override string SFXKey => "sfx_player_dash";
}
