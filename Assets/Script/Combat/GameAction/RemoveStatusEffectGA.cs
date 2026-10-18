using System.Collections.Generic;

public class RemoveStatusEffectGA : GameAction, IHaveCaster
{
    public StatusEffectType StatusEffectType { get; }
    public int StackCount { get; }
    public List<CombatantView> Targets { get; }
    public CombatantView Caster { get; }

    public RemoveStatusEffectGA(StatusEffectType statusEffectType, int stackCount,
        List<CombatantView> targets, CombatantView caster)
    {
        StatusEffectType = statusEffectType;
        StackCount = stackCount;
        Targets = targets == null ? new() : new(targets);
        Caster = caster;
    }
}
