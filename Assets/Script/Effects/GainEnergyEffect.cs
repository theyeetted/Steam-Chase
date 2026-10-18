using System.Collections.Generic;
using UnityEngine;

/// <summary>Grant Energy to the hero executing this effect, regardless of the visible hand.</summary>
[System.Serializable]
public class GainEnergyEffect : Effect
{
    [Tooltip("Energy gained by the caster. Card grants may exceed the normal turn refill of 3.")]
    [SerializeField, Min(0)] private int energyAmount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (energyAmount <= 0 || caster == null || caster.CurrentHealth <= 0 ||
            HeroSystem.Instance == null || !HeroSystem.Instance.TryGetHeroIndex(caster, out _))
            return null;

        return new GainEnergyGA(energyAmount, caster);
    }
}
