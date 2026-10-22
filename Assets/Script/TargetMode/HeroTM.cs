using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HeroTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        // Targets both heroes that are alive 
        List<CombatantView> targets = new();

        PlayerView firstHero = HeroSystem.Instance.playerView;
        PlayerView secondHero = HeroSystem.Instance.secondPlayerView;

        if (firstHero != null && firstHero.CurrentHealth > 0)
            targets.Add(firstHero);

        if (secondHero != null && secondHero.CurrentHealth > 0)
            targets.Add(secondHero);

        return targets;
    }
}
