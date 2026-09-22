namespace Steamchasing.Cards
{
    public enum CardRarity { Common, Rare, Legend, Unique }
    public enum CardType { Attack, Skill, Power }
    public enum CharacterID { Whiskey, Ade }
    public enum DeckType { Deck1, Deck2, Deck3 }
    public enum TargetType { None, SingleEnemy, AllEnemies, Self, SingleAlly, AllAllies, RandomEnemy, RandomEnemies }

    // CardEffect.type is serialized as this enum, so Unity automatically
    // displays every value below in the CardData -> Effects -> Type dropdown.
    public enum EffectType
    {
        Damage,
        Block,
        Heal,
        Draw,
        GainEnergy,

        ApplyVulnerable,
        ApplyAggravated,
        ApplyStress,
        ApplyShock,
        ApplyMark,
        ApplyAttackUp,
        ApplyAttackDown,

        FillStunGauge,

        ExtraAttack,
        FollowUpAttack,

        Retaliate,

        Exhaust,
        Discard,
        Repeat,
        ModifyCost,
        Disintegrate,

        // Immediately detonates current Stress without consuming it.
        // effect.value = percent of detonation damage converted into Stun gauge.
        TriggerStressDetonation,
        DoubleStress
    }
}
