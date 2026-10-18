public class CounterAttackGA : GameAction, IHaveCaster
{
    public CombatantView Caster { get; }
    public EnemyView Target { get; }
    public int Damage { get; }
    public int AdditionalStunPercent { get; }

    public CounterAttackGA(PlayerView caster, EnemyView target, int damage, int additionalStunPercent = 0)
    {
        Caster = caster;
        Target = target;
        Damage = damage;
        AdditionalStunPercent = UnityEngine.Mathf.Clamp(additionalStunPercent, 0, 100);
    }
}
