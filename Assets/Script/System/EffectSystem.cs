using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    //performer
    void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerfromEffectPerformer);
        ActionSystem.AttachPerformer<RepeatAutoTargetEffectGA>(RepeatAutoTargetEffectPerformer);
        ActionSystem.AttachPerformer<ConditionalEffectGA>(ConditionalEffectPerformer);
        ActionSystem.AttachPerformer<BranchEffectGA>(BranchEffectPerformer);
        ActionSystem.AttachPerformer<SequenceEffectGA>(SequenceEffectPerformer);
        ActionSystem.AttachPerformer<FillStunGaugeGA>(FillStunGaugePerformer);
        ActionSystem.AttachPerformer<AddStunPointsGA>(AddStunPointsPerformer);
        ActionSystem.AttachPerformer<GrantTriggeredEffectGA>(GrantTriggeredEffectPerformer);
        ActionSystem.AttachPerformer<HealGA>(HealPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<RepeatAutoTargetEffectGA>();
        ActionSystem.DetachPerformer<ConditionalEffectGA>();
        ActionSystem.DetachPerformer<BranchEffectGA>();
        ActionSystem.DetachPerformer<SequenceEffectGA>();
        ActionSystem.DetachPerformer<FillStunGaugeGA>();
        ActionSystem.DetachPerformer<AddStunPointsGA>();
        ActionSystem.DetachPerformer<GrantTriggeredEffectGA>();
        ActionSystem.DetachPerformer<HealGA>();
    }

    private IEnumerator PerfromEffectPerformer(PerformEffectGA performEffectGA)
    {
        QueueEffect(performEffectGA.Effect, performEffectGA.Context);
        yield return null;
    }

    private IEnumerator RepeatAutoTargetEffectPerformer(RepeatAutoTargetEffectGA action)
    {
        if (action.EffectWrapper == null || action.EffectWrapper.Effect == null || action.Context == null)
            yield break;

        int repeatCount = action.EffectWrapper.ResolveRepeatCount(action.Context);
        for (int i = 0; i < repeatCount; i++)
        {
            ActionSystem.Instance.AddReaction(new PerformEffectGA(
                action.EffectWrapper.Effect,
                action.Context.ResolveTargets(),
                action.Context.Caster,
                action.Context.ManualTarget));
        }

        yield break;
    }

    private IEnumerator ConditionalEffectPerformer(ConditionalEffectGA action)
    {
        // An unfinished Inspector entry must not become an unconditional effect.
        if (action.Condition == null || action.Effect == null)
            yield break;

        bool filterTargets;
        switch (action.Evaluation)
        {
            case ConditionalEffect.Evaluation.FilterTargets:
                filterTargets = true;
                break;
            case ConditionalEffect.Evaluation.Any:
                filterTargets = false;
                break;
            default:
                yield break;
        }

        if (!EffectConditionEvaluator.TryMatch(action.Condition, action.SubjectTargetMode,
            action.Context, filterTargets, out List<CombatantView> matchingSubjects))
            yield break;

        List<CombatantView> recipients = action.RecipientTargetMode != null
            ? action.Context.ResolveTargets(action.RecipientTargetMode)
            : filterTargets ? matchingSubjects : action.Context.ResolveTargets();

        // One payload, even if several subjects match. The manual selection survives
        // recipient redirection for nested effects that explicitly select it.
        QueueEffect(action.Effect, action.Context.WithTargets(recipients));
        yield break;
    }

    private IEnumerator BranchEffectPerformer(BranchEffectGA action)
    {
        if (action.Condition == null)
            yield break;

        bool matches = EffectConditionEvaluator.TryMatch(action.Condition, action.SubjectTargetMode,
            action.Context, false, out _);
        Effect selectedEffect = matches ? action.TrueEffect : action.FalseEffect;
        if (selectedEffect != null)
        {
            // Resolve recipients only for the selected outcome; never rerun the predicate
            // between steps of its payload (for example, draw followed by an Energy grant).
            QueueEffect(selectedEffect,
                action.Context.WithTargets(action.Context.ResolveTargets(action.RecipientTargetMode)));
        }
        yield break;
    }

    private IEnumerator SequenceEffectPerformer(SequenceEffectGA action)
    {
        foreach (Effect effect in action.Effects)
        {
            if (effect != null)
            {
                // Queue wrappers, not already-built actions. Each child resolves its own
                // conditions and targets after the previous child's entire reaction tree.
                ActionSystem.Instance.AddReaction(new PerformEffectGA(effect, action.Context));
            }
        }
        yield break;
    }

    private IEnumerator FillStunGaugePerformer(FillStunGaugeGA action)
    {
        // Each selected combatant receives one fill, even if a target list contains duplicates.
        HashSet<CombatantView> filledTargets = new();
        foreach (CombatantView target in action.Targets)
        {
            if (target == null || target.CurrentHealth <= 0 || !filledTargets.Add(target))
                continue;

            target.AddStunPercent(action.StunFillPercent);
        }
        yield break;
    }

    private IEnumerator AddStunPointsPerformer(AddStunPointsGA action)
    {
        HashSet<CombatantView> filledTargets = new();
        foreach (CombatantView target in action.Context.ResolveTargets())
        {
            if (target == null || target.CurrentHealth <= 0 || !filledTargets.Add(target))
                continue;
            int points = action.StunPoints;
            if (action.PointsValue != null &&
                !action.PointsValue.TryGetValue(action.Context.WithTargets(new() { target }), out points))
                continue;

            // Reuse cooldown, already-Stunned, maximum gauge and presentation handling.
            target.AddStun(Mathf.Max(0, points));
        }
        yield break;
    }

    private IEnumerator GrantTriggeredEffectPerformer(GrantTriggeredEffectGA action)
    {
        HashSet<CombatantView> grantedTargets = new();
        foreach (CombatantView target in action.Targets)
        {
            if (target != null && target.CurrentHealth > 0 && grantedTargets.Add(target))
                target.GrantTriggeredEffect(action.Rule);
        }
        yield break;
    }

    private IEnumerator HealPerformer(HealGA action)
    {
        if (action.Amount <= 0)
            yield break;

        HashSet<CombatantView> healedTargets = new();
        foreach (CombatantView target in action.Targets)
        {
            if (target != null && target.CurrentHealth > 0 && healedTargets.Add(target))
                target.Heal(action.Amount);
        }
        yield break;
    }

    private static void QueueEffect(Effect effect, EffectContext context)
    {
        GameAction effectAction = effect?.GetGameAction(context);
        if (effectAction != null)
            ActionSystem.Instance.AddReaction(effectAction);
    }
}
