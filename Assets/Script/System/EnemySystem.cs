using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemySystem : SingleTon<EnemySystem>
{

    [SerializeField] private EnemyBoardView enemyBoardView;

    private readonly EnemyAI enemyAI = new EnemyAI();

    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;


    void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);

    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
    }

    public void Setup(List<EnemyData> enemyDatas)
    {
        foreach (var enemyData in enemyDatas)
        {
            enemyBoardView.AddEnemy(enemyData);
        }
    }
    private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        foreach (EnemyView enemy in Enemies.ToList())
        {
            if (enemy == null || enemy.CurrentHealth <= 0)
                continue;

            // STUN: skip this enemy's turn.
            if (enemy.IsStunned)
            {
                enemy.ConsumeStun();
                continue;
            }

            // Remember whether cooldown was active when this turn began
            bool cooldownAtTurnStart = enemy.IsStunOnCooldown;

            // Apply SHOCK
            int shock = enemy.GetStatusEffectStacks(StatusEffectType.SHOCK);
            if (shock > 0) { ActionSystem.Instance.AddReaction(new ApplyShockGA(shock, enemy)); }

            List<PlayerView> heroes = enemyAI.GetLivingHeroes();

            if (heroes.Count > 0 && !enemyAI.TryAct(enemy, heroes)) {
                // If no skill was used, attack a random living hero normally.
                PlayerView target = heroes[Random.Range(0, heroes.Count)];
                ActionSystem.Instance.AddReaction(new AttackHeroGA(enemy, target));
            }

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
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
    }

    // Removes all surviving enemies, waits for their removal animations, then creates fresh enemies from the original data.
    public IEnumerator ResetEnemies(List<EnemyData> enemyDatas)
    {
        List<EnemyView> oldEnemies = new(Enemies);

        foreach (EnemyView enemy in oldEnemies)
        {
            yield return enemyBoardView.RemoveEnemy(enemy);
        }

        foreach (EnemyData enemyData in enemyDatas)
        {
            enemyBoardView.AddEnemy(enemyData);
        }
    }
}
