using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OneEnemyTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        List<CombatantView> targets = new List<CombatantView>();

        if (EnemySystem.Instance == null)
        {
            return targets;
        }

        List<EnemyView> livingEnemies = new List<EnemyView>();

        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null && enemy.CurrentHealth > 0)
            {
                livingEnemies.Add(enemy);
            }
        }

        if (livingEnemies.Count == 0)
        {
            return targets;
        }

        EnemyView chosenEnemy =
            livingEnemies[Random.Range(0, livingEnemies.Count)];

        targets.Add(chosenEnemy);
        return targets;
    }
}


