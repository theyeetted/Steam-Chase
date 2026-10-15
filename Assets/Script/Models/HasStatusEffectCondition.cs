using UnityEngine;

[System.Serializable]
public class HasStatusEffectCondition : EffectCondition
{
    public enum Subject
    {
        Target,
        Caster
    }

    [SerializeField] private Subject subject = Subject.Target;
    [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.VULNERABLE;
    [Tooltip("The subject must have at least this many stacks. A value below one is treated as one.")]
    [SerializeField, Min(1)] private int minimumStacks = 1;
    [Tooltip("Require fewer than Minimum Stacks instead. Missing or dead subjects never pass.")]
    [SerializeField] private bool invert;

    public override bool IsMet(CombatantView target, CombatantView caster)
    {
        CombatantView combatant;
        switch (subject)
        {
            case Subject.Target:
                combatant = target;
                break;
            case Subject.Caster:
                combatant = caster;
                break;
            default:
                return false;
        }

        if (combatant == null || combatant.CurrentHealth <= 0)
            return false;

        bool hasEnoughStacks = combatant.GetStatusEffectStacks(statusEffectType) >= Mathf.Max(1, minimumStacks);
        return invert ? !hasEnoughStacks : hasEnoughStacks;
    }
}
