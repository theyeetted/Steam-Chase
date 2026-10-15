using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Wraps any effect with a rule, checked separately for each living target at execution.
/// The inner effect runs once with all matching targets; no matches means no inner action.
/// With no selected targets, the rule receives null and can check the caster instead.
/// </summary>
/// <remarks>
/// Example card: MannualTargetEffect = DealDamageEffect (5).
/// Add OtherTargetEffects entry: ManualTargetTM + ConditionalEffect.
/// Set Condition = HasStatusEffectCondition (Target, VULNERABLE, minimum 1, invert off),
/// and Effect = DealDamageEffect (5). This queues a separate bonus hit after the first
/// hit and its reactions finish. Existing damage modifiers and Block apply to both.
/// The existing Vulnerable modifier makes each base-5 hit deal 6 before Block.
/// No additional scene component is needed; the existing EffectSystem handles it.
/// </remarks>
[System.Serializable]
public class ConditionalEffect : Effect
{
    [Tooltip("A missing condition skips the inner effect.")]
    [SerializeReference, SR] private EffectCondition condition;
    [Tooltip("Runs with the matching targets when the condition passes. May be another ConditionalEffect.")]
    [SerializeReference, SR] private Effect effect;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        // Defer evaluation until execution so earlier card effects/reactions can change the result.
        return new ConditionalEffectGA(condition, effect, targets, caster);
    }
}
