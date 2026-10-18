public class BranchEffectGA : GameAction, IHaveCaster
{
    public EffectCondition Condition { get; }
    public TargetMode SubjectTargetMode { get; }
    public TargetMode RecipientTargetMode { get; }
    public Effect TrueEffect { get; }
    public Effect FalseEffect { get; }
    public EffectContext Context { get; }
    public CombatantView Caster => Context.Caster;

    public BranchEffectGA(EffectCondition condition, TargetMode subjectTargetMode,
        TargetMode recipientTargetMode, Effect trueEffect, Effect falseEffect, EffectContext context)
    {
        Condition = condition;
        SubjectTargetMode = subjectTargetMode;
        RecipientTargetMode = recipientTargetMode;
        TrueEffect = trueEffect;
        FalseEffect = falseEffect;
        Context = context;
    }
}
