using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockSystem : MonoBehaviour
{
    [SerializeField] private GameObject ShockVFX;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<ApplyShockGA>(ApplyShockPerformer);
        ActionSystem.AttachPerformer<TriggerShockGA>(TriggerShockPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ApplyShockGA>();
        ActionSystem.DetachPerformer<TriggerShockGA>();
    }

    private IEnumerator TriggerShockPerformer(TriggerShockGA triggerShockGA)
    {
        HashSet<CombatantView> triggeredTargets = new();
        foreach (CombatantView target in triggerShockGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0 || !triggeredTargets.Add(target))
                continue;

            // Read stacks when this effect resolves, after earlier effects and their reactions.
            int shockStacks = target.GetStatusEffectStacks(StatusEffectType.SHOCK);
            if (shockStacks > 0)
                ActionSystem.Instance.AddReaction(new ApplyShockGA(shockStacks, target));
        }
        yield break;
    }

    private IEnumerator ApplyShockPerformer(ApplyShockGA applyShockGA)
    {
        CombatantView target = applyShockGA.Target;
        // Queued targets may have died or lost Shock to an earlier card effect.
        if (target == null || target.CurrentHealth <= 0 || applyShockGA.ShockDamage <= 0 ||
            target.GetStatusEffectStacks(StatusEffectType.SHOCK) <= 0)
            yield break;

        if (ShockVFX != null)
            Instantiate(ShockVFX, target.transform.position, Quaternion.identity);
        target.Damage(applyShockGA.ShockDamage);
        target.RemoveStatusEffect(StatusEffectType.SHOCK, 1);
        yield return new WaitForSeconds(1f);

        // A lethal tick must remove the enemy before subsequent card effects resolve.
        if (target != null && target.CurrentHealth <= 0 && target is EnemyView enemy)
            ActionSystem.Instance.AddReaction(new KillEnemyGA(enemy));
    }
}
