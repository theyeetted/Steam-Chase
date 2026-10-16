using System.Collections.Generic;
using UnityEngine;

public class AllEnemiesTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        List<CombatantView> targets = new List<CombatantView>();

        if (EnemySystem.Instance == null)
        {
            return targets;
        }

        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null && enemy.CurrentHealth > 0)
            {
                targets.Add(enemy);
            }
        }

        return targets;
    }
}
