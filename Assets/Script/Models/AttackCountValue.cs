using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackCountValue : EffectValue
{
    [SerializeField] private AttackHistoryQuery history = new();

    public override int GetValue(EffectContext context)
    {
        if (context == null || history == null)
            return 0;

        CombatantView target = context.ManualTarget;
        if (target == null)
        {
            List<CombatantView> targets = context.ResolveTargets();
            // Never silently pick the first member of an area effect as its subject.
            if (targets.Count == 1)
                target = targets[0];
        }
        return history.TryGetCount(target, context.Caster, out int count) ? count : 0;
    }
}
