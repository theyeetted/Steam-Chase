using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class TargetMode
{
    public abstract List<CombatantView> GetTargets(CombatantView caster);

    public virtual List<CombatantView> GetTargets(CombatantView manualTarget)
    {
        // Existing automatic target modes do not need the card's manual selection.
        return GetTargets();
    }
    
}
