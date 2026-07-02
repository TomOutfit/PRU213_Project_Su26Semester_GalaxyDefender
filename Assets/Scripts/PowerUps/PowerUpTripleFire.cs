using UnityEngine;
using System.Collections;

public class PowerUpTripleFire : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        PlayerController pc = ph.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.StartCoroutine(TripleFireRoutine(pc));
        }
    }

    private IEnumerator TripleFireRoutine(PlayerController pc)
    {
        pc.isTripleFireActive = true;
        yield return new WaitForSeconds(10f);
        if (pc != null)
        {
            pc.isTripleFireActive = false;
        }
    }

    protected override int ScoreValue => 120;
    protected override string SFXKey => "sfx_powerup_score";
}
