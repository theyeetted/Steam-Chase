using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>Set each recipient's stack total using a value, through the shared add/remove paths.</summary>
[System.Serializable]
public class TransformStatusEffect : Effect
{
    [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.Stress;
    [Tooltip("Desired total, not additional stacks. Each target is calculated separately; normal status caps still apply.")]
    [SerializeReference, SR] private EffectValue stackValue;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return GetGameAction(new EffectContext(targets, caster));
    }

    public override GameAction GetGameAction(EffectContext context)
    {
        return new TransformStatusGA(statusEffectType, stackValue, context);
    }
}
