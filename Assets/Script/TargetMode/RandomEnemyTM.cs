using System.Collections.Generic;
using UnityEngine;

public class RandomEnemyTM : TargetMode
{
    // For enemy skills that target one random hero
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        CombatantView target = EnemySystem.Instance.Enemies[Random.Range(0, EnemySystem.Instance.Enemies.Count)];
        return new() { target };
    }
}
