using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>Adds flat gauge points. FillStunGaugeEffect remains the percentage-of-maximum operation.</summary>
[System.Serializable]
public class AddStunPointsEffect : Effect
{
    [SerializeField, Min(0)] private int stunPoints;
    [Tooltip("Optional per-target calculation replacing Stun Points. A missing producer result skips that recipient.")]
    [SerializeReference, SR] private EffectValue pointsValue;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return GetGameAction(new EffectContext(targets, caster));
    }

    public override GameAction GetGameAction(EffectContext context)
    {
        return new AddStunPointsGA(stunPoints, pointsValue, context);
    }
}
