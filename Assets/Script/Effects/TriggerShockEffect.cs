using System.Collections.Generic;

/// <summary>Immediately triggers one normal Shock tick on each selected target.</summary>
[System.Serializable]
public class TriggerShockEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new TriggerShockGA(targets);
    }
}
