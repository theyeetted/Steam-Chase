using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class TargetMode
{
    public abstract List<CombatantView> GetTargets(CombatantView caster);

    public virtual List<CombatantView> GetTargets(CombatantView caster, CombatantView manualTarget)
    {
        // Automatic target modes use the caster and ignore the card's manual selection.
        return GetTargets(caster);
    }
}
