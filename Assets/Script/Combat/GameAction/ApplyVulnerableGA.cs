using UnityEngine;

public class ApplyVulnerableGA : GameAction
{
    public CombatantView Target { get; private set; }

    public ApplyVulnerableGA(CombatantView target)
    {

        Target = target;
    }
}
