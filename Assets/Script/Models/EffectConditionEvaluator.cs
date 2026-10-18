using System.Collections.Generic;

/// <summary>Shared live-state evaluation for conditional effects and exclusive branches.</summary>
public static class EffectConditionEvaluator
{
    public static bool TryMatch(EffectCondition condition, TargetMode subjectTargetMode,
        EffectContext context, bool allowTargetlessCaster, out List<CombatantView> matchingSubjects)
    {
        matchingSubjects = new();
        if (condition == null || context == null)
            return false;

        List<CombatantView> subjects = context.ResolveTargets(subjectTargetMode);
        if (subjects.Count == 0)
        {
            // Compatibility only: legacy NoTM + caster condition can still run a targetless
            // payload. An explicit empty subject selector or an empty Any always fails.
            return allowTargetlessCaster && subjectTargetMode == null &&
                condition.IsMet(null, context.Caster, context);
        }

        foreach (CombatantView subject in subjects)
        {
            if (subject != null && subject.CurrentHealth > 0 &&
                condition.IsMet(subject, context.Caster, context))
                matchingSubjects.Add(subject);
        }

        return matchingSubjects.Count > 0;
    }
}
