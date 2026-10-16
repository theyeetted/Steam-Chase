using System.Collections.Generic;
using UnityEngine;

/// <summary>Adds a percentage of each selected target's maximum Stun gauge.</summary>
[System.Serializable]
public class FillStunGaugeEffect : Effect
{
    [Tooltip("Adds this percentage of the target's maximum gauge, not the remaining space. Fractional gauge points round up. Existing Stun cooldown applies.")]
    [SerializeField, Range(0, 100)] private int stunFillPercent = 15;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new FillStunGaugeGA(targets, stunFillPercent);
    }
}
