using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    public static int ModifyStatusDamage(CombatantView caster, StatusEffectType buffType, int amount)
    {
        int damage = Mathf.Max(0, amount);
        if (caster == null || !StatusDamageBuffs.IsDamageBuff(buffType))
            return damage;

        int bonus = caster.GetStatusEffectStacks(buffType);
        return damage + Mathf.Min(bonus, int.MaxValue - damage);
    }

    public static int ModifyOutgoingDamage(CombatantView caster, int amount)
    {
        int damage = Mathf.Max(0, amount);
        if (caster != null && caster.GetStatusEffectStacks(StatusEffectType.ATTACKUP) > 0)
        {
            // Attack Up stacks represent duration; every hit gets the same 25% bonus.
            damage = Mathf.FloorToInt(damage * 1.25f);
        }
        if (caster != null && caster.GetStatusEffectStacks(StatusEffectType.AttackDown) > 0)
        {
            // Apply after Attack Up and round each modifier separately. Stacks are duration.
            damage = Mathf.FloorToInt(damage * 0.75f);
        }
        return damage;
    }

    public static CombatantView GetAggravatedTarget(CombatantView caster)
    {
        if (!(caster is EnemyView) || caster == null ||
            caster.GetStatusEffectStacks(StatusEffectType.Aggravated) <= 0)
            return null;

        CombatantView applier = caster.AggravatedApplier;
        if (applier != null && applier.CurrentHealth > 0)
            return applier;

        // A dead/destroyed applier releases the enemy to use its normal targets.
        caster.RemoveStatusEffect(StatusEffectType.Aggravated,
            caster.GetStatusEffectStacks(StatusEffectType.Aggravated));
        return null;
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.AttachPerformer<RemoveStatusEffectGA>(RemoveStatusEffectPerformer);
        ActionSystem.AttachPerformer<TransformStatusGA>(TransformStatusPerformer);
        ActionSystem.AttachPerformer<TriggerStatusGA>(TriggerStatusPerformer);
        ActionSystem.AttachPerformer<DecayAttackUpGA>(DecayAttackUpPerformer);
        ActionSystem.AttachPerformer<DecayAttackDownGA>(DecayAttackDownPerformer);
        ActionSystem.AttachPerformer<ExpireAggravatedGA>(ExpireAggravatedPerformer);
        ActionSystem.AttachPerformer<DecayStatusDamageBuffsGA>(DecayStatusDamageBuffsPerformer);
        // Retain the explicit action API; automatic phase expiry now has one coordinator.
        ActionSystem.AttachPerformer<DecayCounterGA>(DecayCounterPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<StatusAppliedGA>(StatusAppliedPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<AttackResolvedGA>(AttackResolvedPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<StartPlayerPhaseGA>(StartPlayerPhasePreReaction, ReactionTiming.PRE);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<StatusAppliedGA>(StatusAppliedPostReaction, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<AttackResolvedGA>(AttackResolvedPostReaction, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<StartPlayerPhaseGA>(StartPlayerPhasePreReaction, ReactionTiming.PRE);
        ActionSystem.DetachPerformer<DecayAttackUpGA>();
        ActionSystem.DetachPerformer<DecayAttackDownGA>();
        ActionSystem.DetachPerformer<ExpireAggravatedGA>();
        ActionSystem.DetachPerformer<DecayStatusDamageBuffsGA>();
        ActionSystem.DetachPerformer<DecayCounterGA>();
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.DetachPerformer<RemoveStatusEffectGA>();
        ActionSystem.DetachPerformer<TransformStatusGA>();
        ActionSystem.DetachPerformer<TriggerStatusGA>();
    }

    private IEnumerator TriggerStatusPerformer(TriggerStatusGA action)
    {
        List<DamageResult> results = action.ResultStore.BeginDamage(action.ResultKey);
        HashSet<CombatantView> triggeredTargets = new();
        foreach (CombatantView target in action.Targets)
        {
            if (target == null || target.CurrentHealth <= 0 || !triggeredTargets.Add(target))
                continue;

            // Each child action reads stacks at execution, after earlier targets' reactions.
            if (action.StatusEffectType == StatusEffectType.SHOCK)
            {
                ActionSystem.Instance.AddReaction(new ApplyShockGA(target, action.TriggerStacks, action.ConsumeStacks, results));
            }
            else if (action.StatusEffectType == StatusEffectType.Stress && target is EnemyView enemy)
            {
                ActionSystem.Instance.AddReaction(new DetonateStressGA(enemy, action.TriggerStacks, action.ConsumeStacks, results));
            }
        }
        yield break;
    }

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        // Starting the enemy phase ends the player's turn, before enemies can attack.
        if (HeroSystem.Instance != null)
        {
            QueueAttackUpDecay(HeroSystem.Instance.playerView);
            QueueAttackDownDecay(HeroSystem.Instance.playerView);
            QueueStatusDamageBuffDecay(HeroSystem.Instance.playerView);
        }

        // Resolve Stress after the player's turn, before EnemyTurnPerformer consumes Stun.
        if (EnemySystem.Instance != null)
        {
            foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
            {
                if (enemy != null && enemy.CurrentHealth > 0 && enemy.IsStunned &&
                    enemy.GetStatusEffectStacks(StatusEffectType.Stress) > 0)
                {
                    ActionSystem.Instance.AddReaction(new DetonateStressGA(enemy));
                }
            }
        }
    }

    private void StartPlayerPhasePreReaction(StartPlayerPhaseGA action)
    {
        if (action.Advanced || EnemySystem.Instance == null)
            return;
        // Skills and skipped turns do not execute AttackHeroGA's expiry callback.
        // Remove remaining one-turn Aggravated after all enemy/end-phase actions,
        // before the new player phase can deliver scheduled rewards.
        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null && enemy.GetStatusEffectStacks(StatusEffectType.Aggravated) > 0)
                ActionSystem.Instance.AddReaction(new ExpireAggravatedGA(enemy));
        }
    }

    private IEnumerator DecayCounterPerformer(DecayCounterGA action)
    {
        if (action.Target != null)
            action.Target.AdvanceCounterTurn();
        yield break;
    }

    private void AttackHeroPostReaction(AttackHeroGA attackHeroGA)
    {
        // POST runs after this enemy's attack and all of its damage reactions.
        QueueAttackUpDecay(attackHeroGA.Attacker);
        QueueAttackDownDecay(attackHeroGA.Attacker);
        QueueStatusDamageBuffDecay(attackHeroGA.Attacker);
        EnemyView attacker = attackHeroGA.Attacker;
        if (attacker != null && attacker.GetStatusEffectStacks(StatusEffectType.Aggravated) > 0)
        {
            ActionSystem.Instance.AddReaction(new ExpireAggravatedGA(attacker));
        }
    }

    private static void QueueAttackUpDecay(CombatantView target)
    {
        if (target != null && target.CurrentHealth > 0 &&
            target.GetStatusEffectStacks(StatusEffectType.ATTACKUP) > 0)
        {
            ActionSystem.Instance.AddReaction(new DecayAttackUpGA(target));
        }
    }

    private IEnumerator DecayAttackUpPerformer(DecayAttackUpGA decayAttackUpGA)
    {
        CombatantView target = decayAttackUpGA.Target;
        if (target != null && target.CurrentHealth > 0)
        {
            target.RemoveStatusEffect(StatusEffectType.ATTACKUP, 1);
        }
        yield break;
    }

    private static void QueueAttackDownDecay(CombatantView target)
    {
        if (target != null && target.CurrentHealth > 0 &&
            target.GetStatusEffectStacks(StatusEffectType.AttackDown) > 0)
        {
            ActionSystem.Instance.AddReaction(new DecayAttackDownGA(target));
        }
    }

    private IEnumerator DecayAttackDownPerformer(DecayAttackDownGA decayAttackDownGA)
    {
        CombatantView target = decayAttackDownGA.Target;
        if (target != null && target.CurrentHealth > 0)
        {
            target.RemoveStatusEffect(StatusEffectType.AttackDown, 1);
        }
        yield break;
    }

    private IEnumerator ExpireAggravatedPerformer(ExpireAggravatedGA expireAggravatedGA)
    {
        EnemyView target = expireAggravatedGA.Target;
        if (target != null)
        {
            target.RemoveStatusEffect(StatusEffectType.Aggravated,
                target.GetStatusEffectStacks(StatusEffectType.Aggravated));
        }
        yield break;
    }

    private static void QueueStatusDamageBuffDecay(CombatantView target)
    {
        if (target != null && target.CurrentHealth > 0 && target.HasTimedStatusDamageBuffs)
            ActionSystem.Instance.AddReaction(new DecayStatusDamageBuffsGA(target));
    }

    private IEnumerator DecayStatusDamageBuffsPerformer(DecayStatusDamageBuffsGA action)
    {
        if (action.Target != null && action.Target.CurrentHealth > 0)
            action.Target.AdvanceStatusDamageBuffs();
        yield break;
    }

    private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA addStatusEffectGA)
    {
        foreach (var target in addStatusEffectGA.Targets)
        {
            // An earlier card effect may have killed or destroyed this target.
            if (target == null || target.CurrentHealth <= 0)
                continue;

            ApplyStatus(target, addStatusEffectGA.StatusEffectType, addStatusEffectGA.StackCount,
                addStatusEffectGA.Caster, addStatusEffectGA.DamageBuffTurns, addStatusEffectGA.CounterStunFillPercent);
            yield return null;
        }
    }

    private static void ApplyStatus(CombatantView target, StatusEffectType type, int stacks,
        CombatantView caster, int duration = 0, int counterStunFillPercent = 0)
    {
        // Ordinary application and calculated stack increases share caps, ownership,
        // UI refresh and exactly one notification of the accepted increase.
        if (target.TryAddStatusEffect(type, stacks, caster, duration, out int appliedStacks, counterStunFillPercent))
            ActionSystem.Instance.AddReaction(new StatusAppliedGA(type, appliedStacks, caster, target));
    }

    private IEnumerator TransformStatusPerformer(TransformStatusGA action)
    {
        if (action.StackValue == null)
            yield break;
        HashSet<CombatantView> processed = new();
        foreach (CombatantView target in action.Context.ResolveTargets())
        {
            if (target == null || target.CurrentHealth <= 0 || !processed.Add(target))
                continue;

            EffectContext targetContext = action.Context.WithTargets(new() { target });
            if (!action.StackValue.TryGetValue(targetContext, out int value))
                continue;
            int desired = Mathf.Max(0, value);
            int current = Mathf.Max(0, target.GetStatusEffectStacks(action.StatusEffectType));
            if (desired > current)
                ApplyStatus(target, action.StatusEffectType, desired - current, action.Caster);
            else if (desired < current)
                target.RemoveStatusEffect(action.StatusEffectType, current - desired);
        }
        yield break;
    }

    private IEnumerator RemoveStatusEffectPerformer(RemoveStatusEffectGA action)
    {
        if (action.StackCount <= 0)
            yield break;

        HashSet<CombatantView> processedTargets = new();
        foreach (CombatantView target in action.Targets)
        {
            if (target == null || target.CurrentHealth <= 0 || !processedTargets.Add(target))
                continue;

            // Read after preceding damage/reactions: Bolt Stripper removes remaining Block.
            // Reuse removal so icons, ownership, timed grants, and trigger adapters stay in sync.
            int removed = Mathf.Min(action.StackCount, Mathf.Max(0, target.GetStatusEffectStacks(action.StatusEffectType)));
            if (removed > 0)
                target.RemoveStatusEffect(action.StatusEffectType, removed);
        }
        yield break;
    }

    private void StatusAppliedPostReaction(StatusAppliedGA application)
    {
        QueueTriggeredRules(application);
    }

    private void AttackResolvedPostReaction(AttackResolvedGA attack)
    {
        QueueTriggeredRules(attack);
    }

    private static void QueueTriggeredRules(GameAction combatEvent)
    {
        HeroSystem heroes = HeroSystem.Instance;
        if (heroes == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            PlayerView owner = heroes.GetHero(i);
            if (owner != null && owner.CurrentHealth > 0)
                owner.QueueTriggeredEffects(combatEvent);
        }
    }
}
