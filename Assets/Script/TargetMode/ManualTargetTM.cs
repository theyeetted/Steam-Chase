using System.Collections.Generic;

[System.Serializable]
public class ManualTargetTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        // A manual selection is only available while resolving a targeted card.
        return new();
    }

    public override List<CombatantView> GetTargets(CombatantView caster, CombatantView manualTarget)
    {
        if (manualTarget == null || manualTarget.CurrentHealth <= 0)
            return new();

        return new() { manualTarget };
    }
}
