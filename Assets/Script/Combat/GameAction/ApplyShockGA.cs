using UnityEngine;

public class ApplyShockGA : GameAction
{
    public int ShockDamage {  get; private set; }
    public CombatantView Target { get; private set; }

    public ApplyShockGA(int shockDamage, CombatantView target)
    {
        ShockDamage = shockDamage;
        Target = target;
    }
}
