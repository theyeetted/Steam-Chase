using System.Collections.Generic;

/// <summary>
/// Per-execution targets and ownership. Keep the manual selection separate so a nested
/// effect can inspect the selected enemy even after its parent redirects a reward to self.
/// </summary>
public sealed class EffectContext
{
    private readonly List<CombatantView> targets;

    public CombatantView Caster { get; }
    public CombatantView ManualTarget { get; }
    public EffectResults Results { get; }

    public EffectContext(List<CombatantView> targets, CombatantView caster,
        CombatantView manualTarget = null, EffectResults results = null)
    {
        this.targets = targets == null ? new() : new(targets);
        Caster = caster;
        ManualTarget = manualTarget;
        Results = results ?? new EffectResults();
    }

    public List<CombatantView> ResolveTargets(TargetMode targetMode = null)
    {
        // Resolve explicit selectors at execution, not while the card queues later steps.
        List<CombatantView> resolved = targetMode == null
            ? targets
            : targetMode.GetTargets(Caster, ManualTarget);
        return resolved == null ? new() : new(resolved);
    }

    public EffectContext WithTargets(List<CombatantView> recipients)
    {
        return new EffectContext(recipients, Caster, ManualTarget, Results);
    }
}
