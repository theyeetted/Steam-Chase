using System.Collections.Generic;

public class ConditionalEffectGA : GameAction, IHaveCaster
{
    public EffectCondition Condition { get; }
    public Effect Effect { get; }
    public List<CombatantView> Targets { get; }
    public CombatantView Caster { get; }

    public ConditionalEffectGA(EffectCondition condition, Effect effect,
        List<CombatantView> targets, CombatantView caster)
    {
        Condition = condition;
        Effect = effect;
        // Keep per-play targets off the shared effect, and supply an empty list to targetless effects.
        Targets = targets == null ? new() : new(targets);
        Caster = caster;
    }
}
