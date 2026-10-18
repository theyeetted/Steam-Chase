using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatusStacksValue : EffectValue
{
    public enum Subject { Target, Caster }

    [SerializeField] private Subject subject = Subject.Target;
    [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.Stress;

    public override int GetValue(EffectContext context)
    {
        return TryGetValue(context, out int value) ? value : 0;
    }

    public override bool TryGetValue(EffectContext context, out int value)
    {
        value = 0;
        if (context == null)
            return false;

        CombatantView combatant = null;
        if (subject == Subject.Caster)
            combatant = context.Caster;
        else if (subject == Subject.Target)
        {
            List<CombatantView> targets = context.ResolveTargets();
            // Per-target operations pass one recipient. Do not choose an arbitrary area target.
            if (targets.Count == 1)
                combatant = targets[0];
        }
        if (combatant == null || combatant.CurrentHealth <= 0)
            return false;

        value = Mathf.Max(0, combatant.GetStatusEffectStacks(statusEffectType));
        return true;
    }
}
