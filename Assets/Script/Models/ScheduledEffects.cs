using System.Collections.Generic;

/// <summary>One-shot and combat-long phase payloads. Owned and cleared by HeroSystem.</summary>
public sealed class ScheduledEffects
{
    private sealed class Job
    {
        public TriggeredEffectRule Rule;
        public Effect Payload;
        public CombatantView Owner;
        public object TriggerSource;
        public bool RepeatUntilCombatEnd;
    }

    private readonly List<Job> nextPlayerPhase = new();
    private readonly List<Job> playerPhaseEnd = new();
    private bool playerPhaseClosing;

    internal void ScheduleForPlayerPhaseEnd(Effect payload, CombatantView owner, object triggerSource,
        SchedulePlayerPhaseEndEffect.LifetimeMode lifetime)
    {
        // A payout cannot schedule more end-of-phase work, including into the next round.
        if (playerPhaseClosing || payload == null || owner == null || owner.CurrentHealth <= 0)
            return;
        if (lifetime != SchedulePlayerPhaseEndEffect.LifetimeMode.ThisCombat &&
            lifetime != SchedulePlayerPhaseEndEffect.LifetimeMode.Once)
            return;
        playerPhaseEnd.Add(new Job
        {
            Payload = payload,
            Owner = owner,
            TriggerSource = triggerSource,
            RepeatUntilCombatEnd = lifetime == SchedulePlayerPhaseEndEffect.LifetimeMode.ThisCombat
        });
    }

    internal void QueuePlayerPhaseEnd()
    {
        if (playerPhaseClosing)
            return;
        playerPhaseClosing = true;
        Job[] due = playerPhaseEnd.ToArray();
        // Consume one-shot jobs before delivery; retain combat-long grants even if
        // this phase has no eligible card/target. The phase guard prevents double payout.
        playerPhaseEnd.RemoveAll(job => !job.RepeatUntilCombatEnd ||
            job.Owner == null || job.Owner.CurrentHealth <= 0);
        foreach (Job job in due)
        {
            if (job.Owner == null || job.Owner.CurrentHealth <= 0)
                continue;
            // Each activation gets a fresh action/context. Card selection occurs in
            // its payload performer, after all earlier jobs and their reactions.
            PerformEffectGA action = new(job.Payload, new() { job.Owner }, job.Owner);
            action.TriggerSource = job.TriggerSource;
            ActionSystem.Instance.AddReaction(action);
        }
    }

    internal void CancelPlayerPhaseEnd(CombatantView owner)
    {
        playerPhaseEnd.RemoveAll(job => job.Owner == owner);
    }

    internal void ScheduleForNextPlayerPhase(TriggeredEffectRule rule, CombatantView owner, object triggerSource)
    {
        if (rule == null || !rule.IsConfigured || owner == null || owner.CurrentHealth <= 0)
            return;
        nextPlayerPhase.Add(new Job { Rule = rule, Owner = owner, TriggerSource = triggerSource });
    }

    internal void QueueNextPlayerPhase()
    {
        playerPhaseClosing = false;
        Job[] due = nextPlayerPhase.ToArray();
        // Remove before executing: reentry cannot pay a job twice, and jobs created by
        // a payout belong to the following player phase rather than this batch.
        nextPlayerPhase.Clear();
        foreach (Job job in due)
        {
            if (job.Owner == null || job.Owner.CurrentHealth <= 0)
                continue;
            PerformEffectGA action = job.Rule.CreateAction(job.Owner);
            action.TriggerSource = job.TriggerSource;
            ActionSystem.Instance.AddReaction(action);
        }
    }

    public void Clear()
    {
        nextPlayerPhase.Clear();
        playerPhaseEnd.Clear();
        playerPhaseClosing = false;
    }
}
