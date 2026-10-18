/// <summary>
/// A resolved positive status application, published after the shared mutation succeeds.
/// This notification does not apply stacks again and needs no performer.
/// </summary>
public sealed class StatusAppliedGA : GameAction, IHaveCaster
{
    public StatusEffectType StatusEffectType { get; }
    public int AppliedStacks { get; }
    public CombatantView Caster { get; }
    public CombatantView Target { get; }

    public StatusAppliedGA(StatusEffectType type, int appliedStacks,
        CombatantView caster, CombatantView target)
    {
        StatusEffectType = type;
        AppliedStacks = appliedStacks;
        Caster = caster;
        Target = target;
    }
}
