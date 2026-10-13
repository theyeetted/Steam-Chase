using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySystem : SingleTon<EnemySystem>
{
    
    [SerializeField] private EnemyBoardView enemyBoardView;

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
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            int shockStacks = enemy.GetStatusEffectStacks(StatusEffectType.SHOCK);
            if(shockStacks > 0)
            {
                ApplyShockGA applyShockGA = new(shockStacks, enemy);
                ActionSystem.Instance.AddReaction(applyShockGA);
            }
            AttackHeroGA attackHeroGA = new(enemy);
            ActionSystem.Instance.AddReaction(attackHeroGA);
        }
        yield return null;
    }

    private IEnumerator AttackHeroPerformer(AttackHeroGA attackHeroGA)
    {
        EnemyView attacker = attackHeroGA.Attacker;
        if (attacker == null || attacker.CurrentHealth <= 0)
            yield break;

        CombatantView normalTarget = HeroSystem.Instance != null ? HeroSystem.Instance.playerView : null;
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
}
