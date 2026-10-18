using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Run payloads in list order, each after the previous payload and all its reactions.
/// A condition outside this sequence gates the whole list once; a condition inside
/// the list reads live state when its own step executes.
/// </summary>
/// <remarks>
/// Socket to 'Em! and Opportunist use the confirmed after-damage timing: damage first,
/// then ConditionalEffect with Recipient Target Mode = SelfTM. A defeated subject fails
/// the bonus condition; Stun caused by the preceding hit can qualify Opportunist.
/// </remarks>
[System.Serializable]
public class SequenceEffect : Effect
{
    [Tooltip("Each effect inherits the same targets, caster, manual selection and named results. Give producers unique result keys within this sequence. Empty entries are skipped.")]
    [SerializeReference, SR] private List<Effect> effects = new();

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return GetGameAction(new EffectContext(targets, caster));
    }

    public override GameAction GetGameAction(EffectContext context)
    {
        return new SequenceEffectGA(effects, context);
    }
}

[System.Serializable]
public class RepeatFirstAttackCardEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new RepeatFirstAttackCardGA();
    }
}
