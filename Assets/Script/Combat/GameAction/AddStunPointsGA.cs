public sealed class AddStunPointsGA : GameAction, IHaveCaster
{
    public int StunPoints { get; }
    public EffectValue PointsValue { get; }
    public EffectContext Context { get; }
    public CombatantView Caster => Context.Caster;

    public AddStunPointsGA(int stunPoints, EffectValue pointsValue, EffectContext context)
    {
        StunPoints = stunPoints;
        PointsValue = pointsValue;
        Context = context;
    }
}
