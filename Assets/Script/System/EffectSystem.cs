using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    //performer
    void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerfromEffectPerformer);
        ActionSystem.AttachPerformer<ConditionalEffectGA>(ConditionalEffectPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<ConditionalEffectGA>();
    }

    private IEnumerator PerfromEffectPerformer(PerformEffectGA performEffectGA)
    {
        QueueEffect(performEffectGA.Effect, performEffectGA.Targets, performEffectGA.Caster);
        yield return null;
    }

    private IEnumerator ConditionalEffectPerformer(ConditionalEffectGA action)
    {
        // An unfinished Inspector entry must not become an unconditional effect.
        if (action.Condition == null || action.Effect == null)
            yield break;

        List<CombatantView> matchingTargets = new();
        if (action.Targets.Count == 0)
        {
            // Supports caster conditions on targetless effects such as drawing cards.
            if (!action.Condition.IsMet(null, action.Caster))
                yield break;
        }
        else
        {
            foreach (CombatantView target in action.Targets)
            {
                // Previous effects and their reactions may have killed/destroyed queued targets.
                if (target != null && target.CurrentHealth > 0 && action.Condition.IsMet(target, action.Caster))
                    matchingTargets.Add(target);
            }

            if (matchingTargets.Count == 0)
                yield break;
        }

        // Preserve the caster and one action per effect, including nested conditional effects.
        QueueEffect(action.Effect, matchingTargets, action.Caster);
        yield break;
    }

    private static void QueueEffect(Effect effect, List<CombatantView> targets, CombatantView caster)
    {
        GameAction effectAction = effect?.GetGameAction(targets, caster);
        if (effectAction != null)
            ActionSystem.Instance.AddReaction(effectAction);
    }
}
