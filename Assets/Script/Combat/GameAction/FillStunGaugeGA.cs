using System.Collections.Generic;
using UnityEngine;

public class FillStunGaugeGA : GameAction
{
    public List<CombatantView> Targets { get; }
    public int StunFillPercent { get; }

    public FillStunGaugeGA(List<CombatantView> targets, int stunFillPercent)
    {
        Targets = targets == null ? new() : new(targets);
        StunFillPercent = Mathf.Clamp(stunFillPercent, 0, 100);
    }
}
