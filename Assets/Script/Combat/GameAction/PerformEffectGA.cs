using System.Collections.Generic;
using UnityEngine;

public class PerformEffectGA : GameAction
{
    public Effect Effect {  get; set; }
    public List<CombatantView> Targets { get; set; }
    public CombatantView Caster { get; set; } // Who cast the skill

    public PerformEffectGA(Effect effect, List<CombatantView> targets, CombatantView caster)
    {
        Effect = effect;
        Targets = targets == null ? null : new(targets);
        Caster = caster;
    }
}
