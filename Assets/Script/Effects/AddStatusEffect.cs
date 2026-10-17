using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AddStatusEffect : Effect
{
    [SerializeField] private StatusEffectType statusEffectType;
    [SerializeField] private int stackCount;

    public StatusEffectType StatusEffectType => statusEffectType;
    public int StackCount => stackCount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new AddStatusEffectGA(statusEffectType, stackCount, targets, caster);
    }
}
