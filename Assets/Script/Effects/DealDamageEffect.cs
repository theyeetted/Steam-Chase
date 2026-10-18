using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

[System.Serializable]
public class DealDamageEffect : Effect
{
    [Tooltip("Fixed base damage when Damage Value is empty. Existing cards retain this value.")]
    [SerializeField] private int damageAmount;
    [Tooltip("Optional calculated base damage, resolved once before this attack's first hit. Replaces Damage Amount.")]
    [SerializeReference, SR] private EffectValue damageValue;

    public int DamageAmount => damageAmount;
    public bool HasCalculatedDamage => damageValue != null;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return GetGameAction(new EffectContext(targets, caster));
    }

    public override GameAction GetGameAction(EffectContext context)
    {
        return new DealDamageGA(damageAmount, context.ResolveTargets(), context.Caster,
            amountSource: damageValue, valueContext: context);
    }
}
