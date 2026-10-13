using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
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
            target.Damage(damage);

            Instantiate(damageVFX, target.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(0.15f);
            if(target.CurrentHealth <= 0)
            {
                if(target is EnemyView enemyView)
                {
                    KillEnemyGA killEnemyGA = new(enemyView);
                    ActionSystem.Instance.AddReaction(killEnemyGA);

                }
                else
                {
                    //Open Game Over Scene
                }
            }
        }
    }
}
