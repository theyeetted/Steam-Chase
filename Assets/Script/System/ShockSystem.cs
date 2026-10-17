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
        if (target == null || target.CurrentHealth <= 0)
            yield break;

        int shockStacks = target.GetStatusEffectStacks(StatusEffectType.SHOCK);
        if (shockStacks <= 0)
            yield break;

        int damage = applyShockGA.UseCurrentStacks
            ? (applyShockGA.TriggerStacks == StatusTriggerStackMode.One ? 1 : shockStacks)
            : applyShockGA.ShockDamage;
        if (damage <= 0)
            yield break;

        int consumedStacks = applyShockGA.ConsumeStacks switch
        {
            StatusStackConsumption.One => 1,
            StatusStackConsumption.All => shockStacks,
            _ => 0
        };

        if (ShockVFX != null)
            Instantiate(ShockVFX, target.transform.position, Quaternion.identity);
        target.Damage(damage);
        if (consumedStacks > 0)
            target.RemoveStatusEffect(StatusEffectType.SHOCK, consumedStacks);
        yield return new WaitForSeconds(1f);

        // A lethal tick must remove the enemy before subsequent card effects resolve.
        if (target != null && target.CurrentHealth <= 0 && target is EnemyView enemy)
            ActionSystem.Instance.AddReaction(new KillEnemyGA(enemy));
    }
}
