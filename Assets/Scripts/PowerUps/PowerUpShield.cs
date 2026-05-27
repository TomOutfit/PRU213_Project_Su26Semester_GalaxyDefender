using UnityEngine;

public class PowerUpShield : PowerUp
{
    public override void ApplyEffect(PlayerHealth ph)
    {
        ph.AddShield(25);
    }
}
