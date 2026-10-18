public sealed class TransformStatusGA : GameAction, IHaveCaster
{
    public StatusEffectType StatusEffectType { get; }
    public EffectValue StackValue { get; }
    public EffectContext Context { get; }
    public CombatantView Caster => Context.Caster;

    public TransformStatusGA(StatusEffectType statusEffectType, EffectValue stackValue, EffectContext context)
    {
        StatusEffectType = statusEffectType;
        StackValue = stackValue;
        Context = context;
    }
}
