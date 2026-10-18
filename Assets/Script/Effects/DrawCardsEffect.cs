using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DrawCardsEffect : Effect
{
    // Zero preserves the behavior of existing serialized card assets.
    public enum Recipient { FirstHero, Caster }

    [SerializeField] private int drawAmount;
    [Tooltip("First Hero preserves existing cards. Caster draws for the hero executing this effect or owning the rule.")]
    [SerializeField] private Recipient recipient = Recipient.FirstHero;

    public DrawCardsEffect() { }

    public DrawCardsEffect(int amount, Recipient recipient)
    {
        drawAmount = amount;
        this.recipient = recipient;
    }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (drawAmount <= 0)
            return null;
        if (recipient == Recipient.FirstHero)
            return new DrawCardsGA(drawAmount);
        if (recipient != Recipient.Caster || caster == null || caster.CurrentHealth <= 0 ||
            HeroSystem.Instance == null || !HeroSystem.Instance.TryGetHeroIndex(caster, out int heroIndex))
            return null;

        return new DrawCardsGA(drawAmount, heroIndex);
    }
}
