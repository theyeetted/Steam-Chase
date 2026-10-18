using UnityEngine;

/// <summary>
/// Tests saved hit facts, including lethal hits, rather than rechecking surviving enemies.
/// Use SelfTM as the live condition subject when the result gates an owner reward.
/// </summary>
[System.Serializable]
public class AnyHitResultCondition : EffectCondition
{
    [SerializeField] private string resultKey;
    [Tooltip("Require at least one hit whose captured condition passed. Off accepts any recorded hit.")]
    [SerializeField] private bool requireCapturedCondition = true;

    public override bool IsMet(CombatantView target, CombatantView caster)
    {
        // Named results are never inferred from unrelated live state.
        return false;
    }

    public override bool IsMet(CombatantView target, CombatantView caster, EffectContext context)
    {
        if (context == null || !context.Results.TryGetDamage(resultKey, out var results))
            return false;
        foreach (DamageResult result in results)
        {
            if (!requireCapturedCondition || result.ConditionMatched)
                return true;
        }
        return false;
    }
}
