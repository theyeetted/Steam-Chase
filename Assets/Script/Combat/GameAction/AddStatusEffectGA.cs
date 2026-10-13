using System.Collections.Generic;
using UnityEngine;

public class AddStatusEffectGA : GameAction, IHaveCaster
{
    public StatusEffectType StatusEffectType {  get; private set; }
    public int StackCount { get; private set; }
    public CombatantView Caster { get; private set; }

    public List<CombatantView> Targets { get; private set; }
    public AddStatusEffectGA(StatusEffectType statusEffectType, int stackCount, List<CombatantView> targets, CombatantView caster = null)
    {
        StatusEffectType = statusEffectType;
        StackCount = stackCount;
        Targets = targets;
        Caster = caster;
    }
}
