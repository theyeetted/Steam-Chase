
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SelfTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        List<CombatantView> targets = new List<CombatantView>();

        if (caster != null && caster.CurrentHealth > 0)
        {
            targets.Add(caster);
        }

        return targets;
    }
}
