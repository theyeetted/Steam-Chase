using System.Collections.Generic;
using UnityEngine;

/// <summary>Reads the named producer's one hit on the current recipient.</summary>
[System.Serializable]
public class DamageResultValue : EffectValue
{
    public enum Field { DamageBeforeBlock, HealthLost }

    [SerializeField] private string resultKey;
    [SerializeField] private Field field = Field.DamageBeforeBlock;

    public override int GetValue(EffectContext context)
    {
        return TryGetValue(context, out int value) ? value : 0;
    }

    public override bool TryGetValue(EffectContext context, out int value)
    {
        value = 0;
        if (context == null || !context.Results.TryGetDamage(resultKey, out var results))
            return false;
        List<CombatantView> targets = context.ResolveTargets();
        if (targets.Count != 1 || targets[0] == null)
            return false;

        int targetId = targets[0].GetInstanceID();
        DamageResult match = null;
        foreach (DamageResult result in results)
        {
            if (result.TargetId != targetId)
                continue;
            // Multiple hits need an explicit aggregation policy, not an arbitrary last hit.
            if (match != null)
                return false;
            match = result;
        }
        if (match == null)
            return false;
        switch (field)
        {
            case Field.DamageBeforeBlock: value = match.DamageBeforeBlock; return true;
            case Field.HealthLost: value = match.HealthLost; return true;
            default: return false;
        }
    }
}
