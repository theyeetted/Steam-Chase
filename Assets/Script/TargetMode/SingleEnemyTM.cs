
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SingleEnemyTM : TargetMode
{
    // Target one random living enemy. AIt also handles the case where no enemies remain.
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        List<EnemyView> aliveEnemies = new();

        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null && enemy.CurrentHealth > 0)
                aliveEnemies.Add(enemy);
        }

        if (aliveEnemies.Count == 0)
            return new List<CombatantView>();

        int index = Random.Range(0, aliveEnemies.Count);

        return new List<CombatantView> { aliveEnemies[index] };
    }
}
