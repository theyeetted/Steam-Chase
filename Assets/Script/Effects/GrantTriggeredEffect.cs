using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grants each selected living hero an independent rule with its authored lifetime and delivery.
/// Replaying the card adds another grant; existing status adapters retain their own stacking.
/// </summary>
[System.Serializable]
public sealed class GrantTriggeredEffect : Effect
{
    [SerializeField] private TriggeredEffectRule rule = new();

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (rule == null || !rule.IsConfigured)
            return null;

        return new GrantTriggeredEffectGA(rule, targets);
    }
}
