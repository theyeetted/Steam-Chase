using System.Collections.Generic;
using UnityEngine;

public class NoTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        return new List<CombatantView>();
    }
}
