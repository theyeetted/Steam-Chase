using UnityEngine;

/// <summary>Checks the selected enemy's attack history when the conditional effect resolves.</summary>
[System.Serializable]
public class OnAttackedCondition : EffectCondition
{
    public enum TimeWindow
    {
        LastTurn,
        ThisCombat
    }

    [Tooltip("Last Turn checks the most recently completed player or enemy turn. This Combat includes the current turn and earlier effects on this card. Blocked attacks count; Shock and Stress do not.")]
    [SerializeField] private TimeWindow timeWindow = TimeWindow.LastTurn;

    public override bool IsMet(CombatantView target, CombatantView caster)
    {
        // This condition is enemy-only; missing, destroyed, or defeated targets never pass.
        if (target == null || !(target is EnemyView) || target.CurrentHealth <= 0)
            return false;

        return timeWindow switch
        {
            TimeWindow.LastTurn => target.WasAttackedLastTurn,
            TimeWindow.ThisCombat => target.WasAttackedThisCombat,
            _ => false,
        };
    }
}
