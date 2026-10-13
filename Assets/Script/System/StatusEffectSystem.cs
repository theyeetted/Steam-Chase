using System.Collections;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    public static int ModifyOutgoingDamage(CombatantView caster, int amount)
    {
        int damage = Mathf.Max(0, amount);
        if (caster != null && caster.GetStatusEffectStacks(StatusEffectType.ATTACKUP) > 0)
        {
            // Attack Up stacks represent duration; every hit gets the same 25% bonus.
            damage = Mathf.FloorToInt(damage * 1.25f);
        }
        return damage;
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.AttachPerformer<DecayAttackUpGA>(DecayAttackUpPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
        ActionSystem.DetachPerformer<DecayAttackUpGA>();
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
    }

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        // Starting the enemy phase ends the player's turn, before enemies can attack.
        if (HeroSystem.Instance != null)
        {
            QueueAttackUpDecay(HeroSystem.Instance.playerView);
        }
    }

    private void AttackHeroPostReaction(AttackHeroGA attackHeroGA)
    {
        // POST runs after this enemy's attack and all of its damage reactions.
        QueueAttackUpDecay(attackHeroGA.Attacker);
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

    private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA addStatusEffectGA)
    {
        foreach (var target in addStatusEffectGA.Targets)
        {
            // An earlier card effect may have killed or destroyed this target.
            if (target == null || target.CurrentHealth <= 0)
                continue;

            target.AddStatusEffect(addStatusEffectGA.StatusEffectType, addStatusEffectGA.StackCount);
            yield return null;
        }
    }
}
