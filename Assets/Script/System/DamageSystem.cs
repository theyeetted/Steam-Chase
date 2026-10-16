using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;
    [SerializeField] private GameObject gameOverPanel;

    private bool gameOverTriggered;

    private void Start()
    {
        // Hide the panel until both heroes have been defeated.
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
        ActionSystem.AttachPerformer<FollowUpAttackGA>(FollowUpAttackPerformer);
        ActionSystem.AttachPerformer<ExtraAttackGA>(ExtraAttackPerformer);
        ActionSystem.AttachPerformer<DetonateStressGA>(DetonateStressPerformer);
        ActionSystem.AttachPerformer<CounterAttackGA>(CounterAttackPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(AdvanceEnemyAttackHistory, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(AdvanceEnemyAttackHistory, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
        ActionSystem.DetachPerformer<FollowUpAttackGA>();
        ActionSystem.DetachPerformer<ExtraAttackGA>();
        ActionSystem.DetachPerformer<DetonateStressGA>();
        ActionSystem.DetachPerformer<CounterAttackGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(AdvanceEnemyAttackHistory, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(AdvanceEnemyAttackHistory, ReactionTiming.POST);
    }

    private void AdvanceEnemyAttackHistory(EnemyTurnGA action)
    {
        // PRE ends the player turn; POST ends the enemy turn, matching the shared turn flow.
        // Both boundaries matter: Last Turn can refer to either side's completed turn.
        if (EnemySystem.Instance == null)
            return;

        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null)
                enemy.AdvanceAttackHistory();
        }
    }

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        if (gameOverTriggered)
            yield break;

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
            if (gameOverTriggered)
                yield break;

            if (target == null || target.CurrentHealth <= 0)
                continue;

            int damage = outgoingDamage;
            if (aggravatedTarget != null && target == aggravatedTarget)
                damage = Mathf.FloorToInt(damage * 0.85f);
            damage = VulnerableSystem.ModifyIncomingDamage(target, damage);
            // Apply the stunned-target bonus after the ordinary damage modifiers.
            if (target.IsStunned)
                damage = Mathf.CeilToInt(damage * 1.2f);

            // Stun buildup uses the original amount, before bonuses or Block.
            int stunAmount = Mathf.FloorToInt(dealDamageGA.Amount * 0.4f);
            // Record the actual target after redirection, before Block can absorb the hit.
            // Extra Attack also resolves through this path.
            target.RecordAttack();
            yield return ApplyDamage(target, damage, dealDamageGA.Caster, stunAmount);
            // A landed ordinary attack triggers Mark even when Block absorbs the hit.
            // Follow-Up damage and direct status ticks never enter this trigger path.
            QueueMarkFollowUp(target);
            QueueCounterAttack(target, dealDamageGA.Caster);
        }
    }

    private IEnumerator ExtraAttackPerformer(ExtraAttackGA extraAttackGA)
    {
        if (gameOverTriggered)
            yield break;

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
        if (gameOverTriggered)
            yield break;

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
            if (gameOverTriggered)
                yield break;

            if (target == null || target.CurrentHealth <= 0)
                continue;

            // Base four, the dedicated card bonus, and the matching status bonus; Block still applies.
            // Includes Mark-triggered Follow-Up attacks during either side's turn.
            target.RecordAttack();
            yield return ApplyDamage(target, damage, caster, stunFillPercent: followUpAttackGA.StunFillPercent);
            QueueCounterAttack(target, caster);
        }
    }

    private static void QueueCounterAttack(CombatantView defender, CombatantView attacker)
    {
        // Only enemy attacks against a surviving hero trigger Counter. Status ticks never call here.
        if (!(defender is PlayerView hero) || hero == null || hero.CurrentHealth <= 0 ||
            !(attacker is EnemyView enemy) || enemy == null || enemy.CurrentHealth <= 0)
            return;

        int damage = hero.GetStatusEffectStacks(StatusEffectType.Counter);
        if (damage > 0)
            ActionSystem.Instance.AddReaction(new CounterAttackGA(hero, enemy, damage));
    }

    private IEnumerator CounterAttackPerformer(CounterAttackGA action)
    {
        // Earlier hit reactions may have killed the defender or the original attacker.
        if (gameOverTriggered || action.Caster == null || action.Caster.CurrentHealth <= 0 ||
            action.Target == null || action.Target.CurrentHealth <= 0 || action.Damage <= 0)
            yield break;

        // Normal hero attack: applies damage modifiers, Block, Stun buildup, Mark, and attack history.
        // Counter is hero-only and responds only to enemy casters, so it cannot counter itself.
        ActionSystem.Instance.AddReaction(new DealDamageGA(action.Damage, new() { action.Target }, action.Caster));
        yield break;
    }

    private IEnumerator DetonateStressPerformer(DetonateStressGA action)
    {
        if (gameOverTriggered)
            yield break;

        EnemyView target = action.Target;
        // An earlier reaction may have killed the enemy, cleared Stun, or consumed Stress.
        if (target == null || target.CurrentHealth <= 0 || !target.IsStunned)
            yield break;

        int stressStacks = target.GetStatusEffectStacks(StatusEffectType.Stress);
        if (stressStacks <= 0)
            yield break;

        // One damage per stack plus Stun's 20%, rounded up to whole damage.
        // Exact integer arithmetic avoids floating-point over-rounding (25 stacks = 30 damage).
        int damage = stressStacks + (stressStacks + 4) / 5;
        // Bypass attack/status bonuses, Vulnerable, and Mark triggers. Keep Block, VFX, and death handling.
        yield return ApplyDamage(target, damage, null);
        if (target != null)
            target.RemoveStatusEffect(StatusEffectType.Stress, stressStacks);
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

    private IEnumerator ApplyDamage(CombatantView target, int damage, CombatantView caster, int stunAmount = 0,
        int stunFillPercent = 0)
    {
        target.Damage(damage, caster);
        if (stunAmount > 0)
            target.AddStun(stunAmount);
        if (stunFillPercent > 0)
            target.AddStunPercent(stunFillPercent);

        if (damageVFX != null)
            Instantiate(damageVFX, target.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.15f);
        if (target != null && target.CurrentHealth <= 0)
        {
            if (target is EnemyView enemyView)
                ActionSystem.Instance.AddReaction(new KillEnemyGA(enemyView));
            else if (target is PlayerView)
                CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        HeroSystem heroSystem = HeroSystem.Instance;

        bool firstHeroDead =
            heroSystem.playerView == null ||
            heroSystem.playerView.CurrentHealth <= 0;

        bool secondHeroDead =
            heroSystem.secondPlayerView == null ||
            heroSystem.secondPlayerView.CurrentHealth <= 0;

        // One defeated hero is okay. Both defeated means Game Over.
        if (firstHeroDead && secondHeroDead && !gameOverTriggered)
        {
            gameOverTriggered = true;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Debug.Log("GAME OVER: Both heroes have been defeated.");
        }
    }

    public void ResetGameOverState()
    {
        gameOverTriggered = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}
