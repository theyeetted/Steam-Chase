using System.Collections.Generic;
using UnityEngine;

public class AllEnemies : TargetMode
{
    public override List<CombatantView> GetTargets()
    {
        return new(EnemySystem.Instance.Enemies);
    }
}
