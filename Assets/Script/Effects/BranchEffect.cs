using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Evaluate Any once, then execute only the chosen payload. For Pummel Down, use
/// IsStunnedCondition with DealDamageEffect 16 on true and 5 on false as the manual effect.
/// </summary>
[System.Serializable]
public class BranchEffect : Effect
{
    [Tooltip("A missing condition skips both branches.")]
    [SerializeReference, SR] private EffectCondition condition;
    [Tooltip("Empty inspects the incoming targets. Any living matching subject selects True; an empty set selects False.")]
    [SerializeReference, SR] private TargetMode subjectTargetMode;
    [Tooltip("Empty keeps the incoming recipients for either branch. Set SelfTM for an owner reward.")]
    [SerializeReference, SR] private TargetMode recipientTargetMode;
    [Tooltip("Runs once if any subject matches. Empty means no effect for this outcome.")]
    [SerializeReference, SR] private Effect trueEffect;
    [Tooltip("Runs once if no subjects match. It never runs together with True Effect.")]
    [SerializeReference, SR] private Effect falseEffect;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return GetGameAction(new EffectContext(targets, caster));
    }

    public override GameAction GetGameAction(EffectContext context)
    {
        return new BranchEffectGA(condition, subjectTargetMode, recipientTargetMode,
            trueEffect, falseEffect, context);
    }
}
