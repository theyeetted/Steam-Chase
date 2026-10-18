using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HealEffect : Effect
{
    [Tooltip("Restore up to this much missing HP to each living recipient. Does not revive.")]
    [SerializeField, Min(0)] private int healAmount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return healAmount > 0 ? new HealGA(healAmount, targets, caster) : null;
    }
}
