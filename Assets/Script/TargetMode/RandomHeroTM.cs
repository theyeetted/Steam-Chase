using UnityEngine;
using System.Collections.Generic;

public class RandomHeroTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        List<CombatantView> targets = new List<CombatantView>();

        if (HeroSystem.Instance == null)
        {
            return targets;
        }

        List<PlayerView> livingHeroes = new List<PlayerView>();

        PlayerView firstHero = HeroSystem.Instance.playerView;
        PlayerView secondHero = HeroSystem.Instance.secondPlayerView;

        if (firstHero != null && firstHero.CurrentHealth > 0)
        {
            livingHeroes.Add(firstHero);
        }

        if (secondHero != null && secondHero.CurrentHealth > 0)
        {
            livingHeroes.Add(secondHero);
        }

        if (livingHeroes.Count == 0)
        {
            return targets;
        }

        PlayerView chosenHero = livingHeroes[Random.Range(0, livingHeroes.Count)];
        targets.Add(chosenHero);

        return targets;
    }
}
