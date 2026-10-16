using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OneHeroTM : TargetMode
{
    public override List<CombatantView> GetTargets(CombatantView caster)
    {
        var targets = new List<CombatantView>();

        var heroes = new List<PlayerView>
        {
            HeroSystem.Instance.playerView,
            HeroSystem.Instance.secondPlayerView
        };

        // If hero uses the card: select a different living hero.
        heroes.RemoveAll(h =>
            h == null ||
            h.CurrentHealth <= 0 ||
            (caster is PlayerView && h == caster)
        );

        if (heroes.Count > 0)
            targets.Add(heroes[Random.Range(0, heroes.Count)]);

        return targets;
    }
}
