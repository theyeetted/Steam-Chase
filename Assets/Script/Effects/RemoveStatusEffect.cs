using System.Collections.Generic;
using UnityEngine;

/// <summary>Remove stacks through the shared status path; this is not damage or an attack.</summary>
[System.Serializable]
public class RemoveStatusEffect : Effect
{
    [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.BLOCK;
    [Tooltip("Remove up to this many remaining stacks from each living recipient.")]
    [SerializeField, Min(0)] private int stackCount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return stackCount > 0
            ? new RemoveStatusEffectGA(statusEffectType, stackCount, targets, caster)
            : null;
    }
}
