using UnityEngine;

/// <summary>Checks actual gauge-driven Stun, not stacks of StatusEffectType.Stun.</summary>
[System.Serializable]
public class IsStunnedCondition : EffectCondition
{
    public enum Subject { Target, Caster }

    [SerializeField] private Subject subject = Subject.Target;
    [Tooltip("Require the subject to be not Stunned. Missing or dead subjects never pass.")]
    [SerializeField] private bool invert;

    public override bool IsMet(CombatantView target, CombatantView caster)
    {
        CombatantView combatant = subject switch
        {
            Subject.Target => target,
            Subject.Caster => caster,
            _ => null
        };
        if (combatant == null || combatant.CurrentHealth <= 0)
            return false;

        return invert ? !combatant.IsStunned : combatant.IsStunned;
    }
}
