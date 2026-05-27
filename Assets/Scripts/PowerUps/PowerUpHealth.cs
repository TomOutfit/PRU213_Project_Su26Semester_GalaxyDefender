using UnityEngine;

public class PowerUpHealth : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddHealth(30);
    }
}
