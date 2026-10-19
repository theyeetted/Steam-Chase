using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemySystem : SingleTon<EnemySystem>
{

    [SerializeField] private EnemyBoardView enemyBoardView;

    private readonly EnemyAI enemyAI = new EnemyAI();
    private Coroutine prepareActionsRoutine;

    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;


    void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnFinished, ReactionTiming.POST);

    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnFinished, ReactionTiming.POST);
        ResetActionPlans();
    }

    public void Setup(List<EnemyData> enemyDatas)
    {
        ResetActionPlans();
        foreach (var enemyData in enemyDatas)
        {
            enemyBoardView.AddEnemy(enemyData);
        }
        // MatchSetupSystem has already initialized the heroes. Show turn-one intent now.
        PrepareEnemyActions();
    }

    private void PrepareEnemyActions()
    {
        enemyAI.ClearPlans();
        List<PlayerView> heroes = enemyAI.GetLivingHeroes();
        foreach (EnemyView enemy in Enemies)
            enemyAI.PlanTurn(enemy, heroes);
    }

    private void EnemyTurnFinished(EnemyTurnGA action)
    {
        if (prepareActionsRoutine != null)
            StopCoroutine(prepareActionsRoutine);
        prepareActionsRoutine = StartCoroutine(PrepareNextPlayerTurn());
    }

    private IEnumerator PrepareNextPlayerTurn()
    {
        // POST subscribers still queue draws, Shock and status decay. Wait for the
        // entire action flow so the next intent sees the resulting health/targets.
        do
        {
            yield return null;
        }
        while ((ActionSystem.Instance != null && ActionSystem.Instance.IsPerformingAction) ||
            (GameManager.Instance != null && GameManager.Instance.IsPaused));

        prepareActionsRoutine = null;
        if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
            PrepareEnemyActions();
    }

    private void ResetActionPlans()
    {
        if (prepareActionsRoutine != null)
            StopCoroutine(prepareActionsRoutine);
        prepareActionsRoutine = null;
        enemyAI.ClearPlans();
    }
    private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        // All player-end PRE reactions have finished. Begin one received-attack window
        // here, independent of subscriber registration order or the end-turn UI entry point.
        HeroSystem.Instance?.AttackHistory.BeginEnemyPhase();

        foreach (EnemyView enemy in Enemies.ToList())
        {
            if (enemy == null)
                continue;

            if (enemy.CurrentHealth <= 0)
            {
                enemyAI.ForgetPlan(enemy);
                enemy.ClearActionDisplay();
                continue;
            }

            // STUN: skip this enemy's turn.
            if (enemy.IsStunned)
            {
                enemyAI.ForgetPlan(enemy);
                enemy.ClearActionDisplay();
                enemy.ConsumeStun();
                continue;
            }

            // Remember whether cooldown was active when this turn began
            bool cooldownAtTurnStart = enemy.IsStunOnCooldown;

            // Apply SHOCK
            int shock = enemy.GetStatusEffectStacks(StatusEffectType.SHOCK);
            if (shock > 0) { ActionSystem.Instance.AddReaction(new ApplyShockGA(shock, enemy)); }

            enemyAI.ExecutePlannedTurn(enemy);

            // Cooldown lasts until this enemy has completed
            // one turn after recovering from stun.
            if (cooldownAtTurnStart)
            {
                enemy.EndStunCooldown();
            }

        }

        yield return null;
    }

    private IEnumerator AttackHeroPerformer(AttackHeroGA attackHeroGA)
    {
        EnemyView attacker = attackHeroGA.Attacker;
        if (attacker == null || attacker.CurrentHealth <= 0)
            yield break;

        // Preserve the selected hero, with the original fallback for untargeted calls.
        CombatantView normalTarget = attackHeroGA.Target;
        if (normalTarget == null && HeroSystem.Instance != null)
            normalTarget = HeroSystem.Instance.playerView;
        CombatantView attackTarget = StatusEffectSystem.GetAggravatedTarget(attacker);
        if (attackTarget == null)
            attackTarget = normalTarget;
        if (attackTarget == null || attackTarget.CurrentHealth <= 0)
            yield break;

        float startingX = attacker.transform.position.x;
        float direction = Mathf.Sign(attackTarget.transform.position.x - startingX);
        Tween tween = attacker.transform.DOMoveX(startingX + direction, 0.15f);
        yield return tween.WaitForCompletion();
        if (attacker == null || attacker.CurrentHealth <= 0)
            yield break;
        attacker.transform.DOMoveX(startingX, 0.15f);
        // Keep the normal target available if the applier dies before damage executes.
        // DamageSystem resolves Aggravated again after any intervening reactions.
        DealDamageGA dealDamageGA = new(attacker.AttackPower, new() { normalTarget }, attackHeroGA.Caster);
        ActionSystem.Instance.AddReaction(dealDamageGA);
    }

    private IEnumerator KillEnemyPerformer(KillEnemyGA killEnemyGA)
    {
        enemyAI.ForgetPlan(killEnemyGA.EnemyView);
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
    }

    // Removes all surviving enemies, waits for their removal animations, then creates fresh enemies from the original data.
    public IEnumerator ResetEnemies(List<EnemyData> enemyDatas)
    {
        ResetActionPlans();
        List<EnemyView> oldEnemies = new(Enemies);

        foreach (EnemyView enemy in oldEnemies)
        {
            yield return enemyBoardView.RemoveEnemy(enemy);
        }

        foreach (EnemyData enemyData in enemyDatas)
        {
            enemyBoardView.AddEnemy(enemyData);
        }
        PrepareEnemyActions();
    }
}
