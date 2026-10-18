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

    [Tooltip("Last Turn checks whether the selected enemy performed an attack during the latest enemy turn. This Combat checks whether it received an attack during this combat. Blocked attacks count; Shock and Stress do not.")]
    [SerializeField] private TimeWindow timeWindow = TimeWindow.LastTurn;

    public override bool IsMet(CombatantView target, CombatantView caster)
    {
        // This condition is enemy-only; missing, destroyed, or defeated targets never pass.
        if (!(target is EnemyView enemyTarget) || enemyTarget.CurrentHealth <= 0)
            return false;

        HeroSystem heroSystem = HeroSystem.Instance;
        if (heroSystem == null)
            return false;

        switch (timeWindow)
        {
            case TimeWindow.LastTurn:
                return HasAttack(heroSystem, enemyTarget, AttackHistoryDirection.Performed,
                    AttackHistoryWindow.SinceEnemyPhaseStart);
            case TimeWindow.ThisCombat:
                return HasAttack(heroSystem, enemyTarget, AttackHistoryDirection.Received,
                    AttackHistoryWindow.ThisCombat);
            default:
                // Treat the removed serialized enum value as LastTurn for existing assets.
                return HasAttack(heroSystem, enemyTarget, AttackHistoryDirection.Performed,
                    AttackHistoryWindow.SinceEnemyPhaseStart);
        }
    }

    private static bool HasAttack(HeroSystem heroSystem, EnemyView target,
        AttackHistoryDirection direction, AttackHistoryWindow window)
    {
        return heroSystem.AttackHistory.Count(
            direction,
            window,
            AttackKind.All,
            target.GetInstanceID(),
            bothHeroes: false) > 0;
    }
}
