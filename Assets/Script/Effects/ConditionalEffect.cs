using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Gates a payload using live condition subjects, with independently selected recipients.
/// Defaults preserve the original behavior: filter incoming targets, then run one payload
/// with all matches. Any checks the subject set once and keeps the incoming recipients.
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
    // Zero keeps the behavior of existing serialized cards.
    public enum Evaluation { FilterTargets, Any }

    [Tooltip("A missing condition skips the inner effect.")]
    [SerializeReference, SR] private EffectCondition condition;
    [Tooltip("Runs once when the condition passes, using the recipients below. May contain a SequenceEffect or another condition.")]
    [SerializeReference, SR] private Effect effect;
    [Tooltip("Empty inspects incoming targets. Set AllEnemiesTM to inspect the board or SelfTM to inspect the caster.")]
    [SerializeReference, SR] private TargetMode subjectTargetMode;
    [Tooltip("Filter Targets forwards matching subjects. Any gates one payload with the original recipients; an empty subject set fails.")]
    [SerializeField] private Evaluation evaluation = Evaluation.FilterTargets;
    [Tooltip("Empty uses the evaluation's default recipients. Set SelfTM to reward the caster after checking an enemy.")]
    [SerializeReference, SR] private TargetMode recipientTargetMode;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return GetGameAction(new EffectContext(targets, caster));
    }

    public override GameAction GetGameAction(EffectContext context)
    {
        // Defer evaluation until execution so earlier card effects/reactions can change the result.
        return new ConditionalEffectGA(condition, effect, context,
            subjectTargetMode, evaluation, recipientTargetMode);
    }
}
