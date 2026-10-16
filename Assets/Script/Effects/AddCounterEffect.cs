using System.Collections.Generic;
using UnityEngine;

/// <summary>Grants a hero a counterattack against each enemy that hits them.</summary>
[System.Serializable]
public class AddCounterEffect : Effect
{
    public enum DurationMode
    {
        Turns,
        ThisCombat
    }

    [Tooltip("Base damage of each counterattack. Normal attack modifiers and enemy Block apply. Reapplications add damage with independent durations.")]
    [SerializeField, Min(1)] private int counterDamage = 5;
    [SerializeField] private DurationMode duration = DurationMode.Turns;
    [Tooltip("Number of enemy-turn ends until expiration. One turn covers the next enemy phase. Ignored for This Combat.")]
    [SerializeField, Min(1)] private int durationTurns = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        int turns = duration switch
        {
            DurationMode.Turns => Mathf.Max(1, durationTurns),
            DurationMode.ThisCombat => 0,
            _ => -1,
        };
        if (turns < 0)
            return null;

        return new AddStatusEffectGA(StatusEffectType.Counter, Mathf.Max(0, counterDamage), targets, caster, turns);
    }
}
