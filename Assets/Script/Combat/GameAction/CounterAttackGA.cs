public class CounterAttackGA : GameAction, IHaveCaster
{
    public CombatantView Caster { get; }
    public EnemyView Target { get; }
    public int Damage { get; }

    public CounterAttackGA(PlayerView caster, EnemyView target, int damage)
    {
        Caster = caster;
        Target = target;
        Damage = damage;
    }
}
