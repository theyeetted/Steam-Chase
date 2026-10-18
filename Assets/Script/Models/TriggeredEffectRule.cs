using SerializeReferenceEditor;
using UnityEngine;

/// <summary>
/// Shared authored definition: when a trigger matches, execute an existing effect for
/// independently selected recipients. Runtime grants and event history live elsewhere.
/// </summary>
[System.Serializable]
public sealed class TriggeredEffectRule
{
    // Zero values preserve existing combat-long, immediate, unlimited rules.
    public enum LifetimeMode { ThisCombat, UntilNextPlayerPhase }
    public enum DeliveryMode { Immediate, NextPlayerPhase }

    [SerializeReference, SR] private EffectTrigger trigger;
    [SerializeReference, SR] private TargetMode targetMode = new SelfTM();
    [SerializeReference, SR] private Effect effect;
    [SerializeField] private LifetimeMode lifetime = LifetimeMode.ThisCombat;
    [Tooltip("Zero allows unlimited triggers. One allows one reward per activation, regardless of the number of later matching hits.")]
    [SerializeField, Min(0)] private int triggerLimit;
    [Tooltip("Next Player Phase delivers after the upcoming enemy phase and its normal draw. Queued rewards survive this rule's expiry.")]
    [SerializeField] private DeliveryMode delivery = DeliveryMode.Immediate;

    internal LifetimeMode Lifetime => lifetime;
    internal int TriggerLimit => Mathf.Max(0, triggerLimit);
    internal DeliveryMode Delivery => delivery;

    public bool IsConfigured => trigger != null && targetMode != null && effect != null &&
        (lifetime == LifetimeMode.ThisCombat || lifetime == LifetimeMode.UntilNextPlayerPhase) &&
        (delivery == DeliveryMode.Immediate || delivery == DeliveryMode.NextPlayerPhase);

    public TriggeredEffectRule() { }

    public TriggeredEffectRule(EffectTrigger trigger, TargetMode targetMode, Effect effect)
    {
        this.trigger = trigger;
        this.targetMode = targetMode;
        this.effect = effect;
    }

    internal bool Matches(GameAction combatEvent, CombatantView owner)
    {
        return IsConfigured && trigger.IsMet(combatEvent, owner);
    }

    internal PerformEffectGA CreateAction(CombatantView owner)
    {
        return new PerformEffectGA(effect, targetMode.GetTargets(owner), owner);
    }
}
