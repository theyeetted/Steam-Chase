using System.Collections.Generic;
using UnityEngine;

public class AddStatusEffectGA : GameAction, IHaveCaster
{
    public StatusEffectType StatusEffectType {  get; private set; }
    public int StackCount { get; private set; }
    public CombatantView Caster { get; private set; }
    // Used by damage-buff statuses and Counter. Zero means permanent for this battle.
    public int DamageBuffTurns { get; private set; }
    public int CounterStunFillPercent { get; }

    public List<CombatantView> Targets { get; private set; }
    public AddStatusEffectGA(StatusEffectType statusEffectType, int stackCount, List<CombatantView> targets,
        CombatantView caster = null, int damageBuffTurns = 0, int counterStunFillPercent = 0)
    {
        StatusEffectType = statusEffectType;
        StackCount = stackCount;
        Targets = targets == null ? new() : new(targets);
        Caster = caster;
        DamageBuffTurns = damageBuffTurns;
        CounterStunFillPercent = Mathf.Clamp(counterStunFillPercent, 0, 100);
    }
}
