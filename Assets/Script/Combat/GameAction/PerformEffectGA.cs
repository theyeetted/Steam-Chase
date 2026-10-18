using System.Collections.Generic;
using UnityEngine;

public class PerformEffectGA : GameAction
{
    public Effect Effect {  get; set; }
    public List<CombatantView> Targets { get; set; }
    public CombatantView Caster { get; set; } // Who cast the skill
    public CombatantView ManualTarget { get; }
    private readonly EffectResults results;
    public EffectContext Context => new(Targets, Caster, ManualTarget, results);

    public PerformEffectGA(Effect effect, List<CombatantView> targets, CombatantView caster,
        CombatantView manualTarget = null)
    {
        Effect = effect;
        Targets = targets == null ? null : new(targets);
        Caster = caster;
        ManualTarget = manualTarget;
        results = new EffectResults();
    }

    public PerformEffectGA(Effect effect, EffectContext context)
        : this(effect, context.ResolveTargets(), context.Caster, context.ManualTarget)
    {
        // Sequence children and recipient changes retain the producer's result store.
        results = context.Results;
    }
}

public class RepeatFirstAttackCardGA : GameAction
{
}

public class RepeatAutoTargetEffectGA : GameAction
{
    public AutoTargetEffect EffectWrapper { get; }
    public EffectContext Context { get; }

    public RepeatAutoTargetEffectGA(AutoTargetEffect effectWrapper, EffectContext context)
    {
        EffectWrapper = effectWrapper;
        Context = context;
    }
}
