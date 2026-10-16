using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "AllEnemies")]
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
