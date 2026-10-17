using System.Collections.Generic;
using UnityEngine;

/// <summary>Immediately deals status damage using one or all stacks, with independent consumption.</summary>
[System.Serializable]
public class TriggerStatusEffect : Effect
{
    public enum StatusKind
    {
        Shock = 0,
        Stress = 1
    }

    [Tooltip("Stress can be triggered on any enemy carrying it; Stun is not required.")]
    [SerializeField] private StatusKind status = StatusKind.Shock;
    [Tooltip("One uses one stack for damage. All uses every stack in a single hit, not repeated ticks. Stress gains 20% damage against stunned enemies, rounded up.")]
    [SerializeField] private StatusTriggerStackMode triggerStacks = StatusTriggerStackMode.All;
    [Tooltip("Stacks removed after damage, independently of how many stacks were used for damage.")]
    [SerializeField] private StatusStackConsumption consumeStacks = StatusStackConsumption.One;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        StatusEffectType type = status == StatusKind.Stress ? StatusEffectType.Stress : StatusEffectType.SHOCK;
        return new TriggerStatusGA(type, targets, triggerStacks, consumeStacks);
    }
}
