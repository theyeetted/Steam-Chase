using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
        ActionSystem.AttachPerformer<FollowUpAttackGA>(FollowUpAttackPerformer);
        ActionSystem.AttachPerformer<ExtraAttackGA>(ExtraAttackPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
        ActionSystem.DetachPerformer<FollowUpAttackGA>();
        ActionSystem.DetachPerformer<ExtraAttackGA>();
    }

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA.Caster is EnemyView enemyCaster &&
            (enemyCaster == null || enemyCaster.CurrentHealth <= 0))
            yield break;

        int outgoingDamage = StatusEffectSystem.ModifyOutgoingDamage(dealDamageGA.Caster, dealDamageGA.Amount);
        CombatantView aggravatedTarget = StatusEffectSystem.GetAggravatedTarget(dealDamageGA.Caster);
        // Replace the target set once, so a multi-target attack hits only the applier once.
        List<CombatantView> targets = aggravatedTarget != null
            ? new List<CombatantView> { aggravatedTarget }
            : dealDamageGA.Targets;
        foreach (var target in targets)
        {
            if (target == null || target.CurrentHealth <= 0)
                continue;

            int damage = outgoingDamage;
            if (aggravatedTarget != null && target == aggravatedTarget)
                damage = Mathf.FloorToInt(damage * 0.85f);
            damage = VulnerableSystem.ModifyIncomingDamage(target, damage);
            yield return ApplyDamage(target, damage);
            // A landed ordinary attack triggers Mark even when Block absorbs the hit.
            // Follow-Up damage and direct status ticks never enter this trigger path.
            QueueMarkFollowUp(target);
        }
    }

    private IEnumerator ExtraAttackPerformer(ExtraAttackGA extraAttackGA)
    {
        CombatantView caster = extraAttackGA.Caster;
        if (caster == null || caster.CurrentHealth <= 0 ||
            caster.GetStatusEffectStacks(StatusEffectType.ExtraAttack) <= 0)
            yield break;

        // Check the actual target before spending a charge. Aggravated can replace
        // the requested targets, and releases the caster if its applier has died.
        CombatantView aggravatedTarget = StatusEffectSystem.GetAggravatedTarget(caster);
        if (aggravatedTarget == null &&
            !extraAttackGA.Targets.Exists(target => target != null && target.CurrentHealth > 0))
            yield break;

        caster.RemoveStatusEffect(StatusEffectType.ExtraAttack, 1);

        // A distinct normal damage action applies all ordinary modifiers and Mark.
        // Keep the original targets so Aggravated can be resolved again at execution.
        ActionSystem.Instance.AddReaction(new DealDamageGA(
            StatusEffectSystem.ModifyStatusDamage(caster, StatusEffectType.ExtraAttackDamageUp, ExtraAttackGA.BaseDamage),
            extraAttackGA.Targets, caster));
        yield break;
    }

    private IEnumerator FollowUpAttackPerformer(FollowUpAttackGA followUpAttackGA)
    {
        CombatantView caster = followUpAttackGA.Caster;
        if (caster == null || caster.CurrentHealth <= 0)
            yield break;
        if (!followUpAttackGA.IsTriggeredByMark &&
            caster.GetStatusEffectStacks(StatusEffectType.FollowUpAttack) <= 0)
            yield break;

        // Mark's off-turn reaction always strikes the marked unit. Ordinary Follow-Up
        // requests still obey Aggravated targeting, but ignore its damage reduction.
        CombatantView aggravatedTarget = followUpAttackGA.IsTriggeredByMark
            ? null
            : StatusEffectSystem.GetAggravatedTarget(caster);
        List<CombatantView> targets = aggravatedTarget != null
            ? new List<CombatantView> { aggravatedTarget }
            : followUpAttackGA.Targets;

        // An action with no living targets does not spend a charge.
        if (!targets.Exists(target => target != null && target.CurrentHealth > 0))
            yield break;

        // Mark has already consumed its stack; it never spends the applier's charges.
        if (!followUpAttackGA.IsTriggeredByMark)
            caster.RemoveStatusEffect(StatusEffectType.FollowUpAttack, 1);
        // Mark reactions use the Mark applier's dedicated bonus. Regular Follow-Up bonuses
        // remain separate; neither path uses ordinary attack modifiers or Vulnerable.
        StatusEffectType damageBuffType = followUpAttackGA.IsTriggeredByMark
            ? StatusEffectType.MarkFollowUpDamageUp
            : StatusEffectType.FollowUpAttackDamageUp;
        int damage = StatusEffectSystem.ModifyStatusDamage(caster, damageBuffType, followUpAttackGA.Damage);
        foreach (var target in targets)
        {
            if (target == null || target.CurrentHealth <= 0)
                continue;

            // Base four, the dedicated card bonus, and the matching status bonus; Block still applies.
            yield return ApplyDamage(target, damage);
        }
    }

    private static void QueueMarkFollowUp(CombatantView target)
    {
        if (target == null || target.CurrentHealth <= 0 ||
            target.GetStatusEffectStacks(StatusEffectType.Mark) <= 0)
            return;

        CombatantView applier = target.MarkApplier;
        if (applier == null || applier.CurrentHealth <= 0)
            return;

        // Capture ownership before the last-stack removal clears it. Consume now
        // so repeated hits cannot queue more reactions than the available stacks.
        target.RemoveStatusEffect(StatusEffectType.Mark, 1);
        ActionSystem.Instance.AddReaction(FollowUpAttackGA.CreateFromMark(target, applier));
    }

    private IEnumerator ApplyDamage(CombatantView target, int damage)
    {
        target.Damage(damage);
        Instantiate(damageVFX, target.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.15f);
        if (target != null && target.CurrentHealth <= 0)
        {
            if (target is EnemyView enemyView)
                ActionSystem.Instance.AddReaction(new KillEnemyGA(enemyView));
            else
            {
                //Open Game Over Scene
            }
        }
    }
}
