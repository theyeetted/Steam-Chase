using UnityEngine;
using System.Collections;

public class VulnerableSystem : MonoBehaviour
{
    public static int ModifyIncomingDamage(CombatantView target, int amount)
    {
        int damage = Mathf.Max(0, amount);
        if (target != null && target.GetStatusEffectStacks(StatusEffectType.VULNERABLE) > 0)
        {
            // Stacks represent duration, not extra damage multipliers.
            damage = Mathf.FloorToInt(damage * 1.25f);
        }
        return damage;
    }

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<ApplyVulnerableGA>(ApplyVulnerablePerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.DetachPerformer<ApplyVulnerableGA>();
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        if (HeroSystem.Instance != null)
        {
            QueueDecay(HeroSystem.Instance.playerView);
        }

        if (EnemySystem.Instance != null)
        {
            foreach (var enemy in EnemySystem.Instance.Enemies)
            {
                QueueDecay(enemy);
            }
        }
    }

    private static void QueueDecay(CombatantView target)
    {
        if (target != null && target.CurrentHealth > 0 &&
            target.GetStatusEffectStacks(StatusEffectType.VULNERABLE) > 0)
        {
            ActionSystem.Instance.AddReaction(new ApplyVulnerableGA(target));
        }
    }

    private IEnumerator ApplyVulnerablePerformer(ApplyVulnerableGA applyVulnerableGA)
    {
        CombatantView target = applyVulnerableGA.Target;
        if (target != null && target.CurrentHealth > 0)
        {
            // This action expires one stack at enemy-turn end; it never deals damage.
            target.RemoveStatusEffect(StatusEffectType.VULNERABLE, 1);
        }
        yield break;
    }
}
