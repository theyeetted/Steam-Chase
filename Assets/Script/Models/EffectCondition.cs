/// <summary>
/// A read-only rule evaluated when a ConditionalEffectGA executes.
/// Implementations must not store per-play state in the shared card definition.
/// </summary>
[System.Serializable]
public abstract class EffectCondition
{
    // Target is null for effects with no selected targets (for example, NoTM + draw).
    // A target-based rule must return false in that case; caster-based rules may pass.
    public abstract bool IsMet(CombatantView target, CombatantView caster);

    // Result-aware conditions use this overload. Existing live conditions keep their API.
    public virtual bool IsMet(CombatantView target, CombatantView caster, EffectContext context)
    {
        return IsMet(target, caster);
    }
}
