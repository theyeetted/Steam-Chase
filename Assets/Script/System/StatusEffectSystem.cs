using System.Collections;
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
        ActionSystem.AttachPerformer<DecayAttackUpGA>(DecayAttackUpPerformer);
        ActionSystem.AttachPerformer<DecayAttackDownGA>(DecayAttackDownPerformer);
        ActionSystem.AttachPerformer<ExpireAggravatedGA>(ExpireAggravatedPerformer);
        ActionSystem.AttachPerformer<DecayStatusDamageBuffsGA>(DecayStatusDamageBuffsPerformer);
        ActionSystem.AttachPerformer<DecayCounterGA>(DecayCounterPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
        ActionSystem.DetachPerformer<DecayAttackUpGA>();
        ActionSystem.DetachPerformer<DecayAttackDownGA>();
        ActionSystem.DetachPerformer<ExpireAggravatedGA>();
        ActionSystem.DetachPerformer<DecayStatusDamageBuffsGA>();
        ActionSystem.DetachPerformer<DecayCounterGA>();
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
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

    private void EnemyTurnPostReaction(EnemyTurnGA action)
    {
        // All enemy attacks and their counters have resolved. Tick each hero once per phase.
        if (HeroSystem.Instance == null)
            return;

        QueueCounterDecay(HeroSystem.Instance.playerView);
        QueueCounterDecay(HeroSystem.Instance.secondPlayerView);
    }

    private static void QueueCounterDecay(PlayerView hero)
    {
        if (hero != null && hero.HasTimedCounter)
            ActionSystem.Instance.AddReaction(new DecayCounterGA(hero));
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

            target.AddStatusEffect(addStatusEffectGA.StatusEffectType, addStatusEffectGA.StackCount,
                addStatusEffectGA.Caster, addStatusEffectGA.DamageBuffTurns);
            yield return null;
        }
    }
}
