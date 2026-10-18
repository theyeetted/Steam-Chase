/// <summary>
/// Notification after a hit has applied damage and Stun. History is already recorded;
/// this action must not deal damage or increment history again. No performer is needed.
/// </summary>
public sealed class AttackResolvedGA : GameAction, IHaveCaster
{
    public ResolvedAttack Attack { get; }
    public CombatantView Caster { get; }
    public CombatantView Target { get; }

    public AttackResolvedGA(ResolvedAttack attack, CombatantView caster, CombatantView target)
    {
        Attack = attack;
        Caster = caster;
        Target = target;
    }
}
