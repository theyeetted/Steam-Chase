using System.Collections.Generic;

/// <summary>
/// Runtime grants owned by one hero. Independent grants can share a
/// definition; replacing an adapter grant keeps its identity for recursion protection.
/// </summary>
public sealed class TriggeredEffectRules
{
    private sealed class Grant
    {
        public object Source;
        public TriggeredEffectRule Rule;
        public long AfterAttackId;
        public int TriggersFired;
    }

    // Preserve grant order explicitly, including when one adapter's strength changes.
    private readonly List<Grant> grants = new();

    public void Add(TriggeredEffectRule rule)
    {
        Set(new object(), rule);
    }

    public void Set(object source, TriggeredEffectRule rule)
    {
        if (source == null)
            return;
        int index = grants.FindIndex(grant => Equals(grant.Source, source));
        if (rule == null || !rule.IsConfigured)
        {
            if (index >= 0)
                grants.RemoveAt(index);
            return;
        }

        if (index >= 0)
            grants[index].Rule = rule;
        else
            grants.Add(new Grant
            {
                Source = source,
                Rule = rule,
                // An already-resolved hit may still have its notification queued.
                // New grants must not react to hits that happened before activation.
                AfterAttackId = HeroSystem.Instance != null ? HeroSystem.Instance.AttackHistory.LastAttackId : 0
            });
    }

    public void QueueReactions(GameAction combatEvent, CombatantView owner)
    {
        if (combatEvent == null || owner == null || owner.CurrentHealth <= 0 || ActionSystem.Instance == null)
            return;

        foreach (Grant grant in grants.ToArray())
        {
            if (combatEvent is AttackResolvedGA resolved &&
                (resolved.Attack == null || resolved.Attack.Id <= grant.AfterAttackId))
                continue;
            if (grant.Rule.Delivery == TriggeredEffectRule.DeliveryMode.NextPlayerPhase && HeroSystem.Instance == null)
                continue;
            if (!grant.Rule.Matches(combatEvent, owner) || !combatEvent.TryRegisterTrigger(grant))
                continue;

            // Consume a limited activation before any payload can produce more events.
            if (grant.Rule.TriggerLimit > 0 && ++grant.TriggersFired >= grant.Rule.TriggerLimit)
                grants.Remove(grant);

            if (grant.Rule.Delivery == TriggeredEffectRule.DeliveryMode.NextPlayerPhase)
            {
                HeroSystem.Instance.ScheduledEffects.ScheduleForNextPlayerPhase(grant.Rule, owner, grant);
                continue;
            }
            // Suppressed recursive triggers must not resolve targets or consume random rolls.
            PerformEffectGA reaction = grant.Rule.CreateAction(owner);
            reaction.TriggerSource = grant;
            ActionSystem.Instance.AddReaction(reaction);
        }
    }

    public void AdvancePlayerPhase()
    {
        grants.RemoveAll(grant => grant.Rule.Lifetime == TriggeredEffectRule.LifetimeMode.UntilNextPlayerPhase);
    }

    public void Clear()
    {
        grants.Clear();
    }
}
