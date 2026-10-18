using System.Collections.Generic;

public class ConditionalEffectGA : GameAction, IHaveCaster
{
    public EffectCondition Condition { get; }
    public Effect Effect { get; }
    public List<CombatantView> Targets => Context.ResolveTargets();
    public CombatantView Caster => Context.Caster;
    public EffectContext Context { get; }
    public TargetMode SubjectTargetMode { get; }
    public ConditionalEffect.Evaluation Evaluation { get; }
    public TargetMode RecipientTargetMode { get; }

    public ConditionalEffectGA(EffectCondition condition, Effect effect,
        List<CombatantView> targets, CombatantView caster)
        : this(condition, effect, new EffectContext(targets, caster))
    {
    }

    public ConditionalEffectGA(EffectCondition condition, Effect effect, EffectContext context,
        TargetMode subjectTargetMode = null,
        ConditionalEffect.Evaluation evaluation = ConditionalEffect.Evaluation.FilterTargets,
        TargetMode recipientTargetMode = null)
    {
        Condition = condition;
        Effect = effect;
        Context = context;
        SubjectTargetMode = subjectTargetMode;
        Evaluation = evaluation;
        RecipientTargetMode = recipientTargetMode;
    }
}
