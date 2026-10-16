using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AllHeroesTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        List<CombatantView> targets = new List<CombatantView>();

        if (HeroSystem.Instance == null)
        {
            return targets;
        }

        PlayerView firstHero = HeroSystem.Instance.playerView;
        PlayerView secondHero = HeroSystem.Instance.secondPlayerView;

        if (firstHero != null && firstHero.CurrentHealth > 0)
        {
            targets.Add(firstHero);
        }

        if (secondHero != null && secondHero.CurrentHealth > 0)
        {
            targets.Add(secondHero);
        }

        return targets;
    }
}
