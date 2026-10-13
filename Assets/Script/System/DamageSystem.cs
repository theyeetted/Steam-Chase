using System.Collections;
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
        int outgoingDamage = StatusEffectSystem.ModifyOutgoingDamage(dealDamageGA.Caster, dealDamageGA.Amount);
        foreach (var target in dealDamageGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0)
                continue;

            int damage = VulnerableSystem.ModifyIncomingDamage(target, outgoingDamage);
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
