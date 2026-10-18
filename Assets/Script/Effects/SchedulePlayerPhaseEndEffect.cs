using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

/// <summary>Schedule an owner-bound payload before shared player-phase hand discard.</summary>
[System.Serializable]
public sealed class SchedulePlayerPhaseEndEffect : Effect
{
    // Zero intentionally upgrades existing Gear overheat entries to the revised combat-long rule.
    public enum LifetimeMode { ThisCombat = 0, Once = 1 }

    [Tooltip("Runs for the caster before hand discard. Dead owners and combat reset cancel it.")]
    [SerializeReference, SR] private Effect effect;
    [Tooltip("This Combat repeats once each player-phase end, even after an earlier phase had no eligible card. Once expires after its first attempt.")]
    [SerializeField] private LifetimeMode lifetime = LifetimeMode.ThisCombat;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return effect == null ? null : new SchedulePlayerPhaseEndGA(effect, caster, lifetime);
    }
}
