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

        // Adds flat damage to each Shock tick event on the affected target.
        // effect.value = additional damage per Shock tick.
        // effect.statusDuration = duration in turns.
        ShockTickDamageUp,

        // Heals the source for effect.value HP per total Shock stack
        // currently active across all living enemies.
        HealPerTotalEnemyShock,

        ApplyMark,
        ApplyAttackUp,
        ApplyAttackDown,

        FillStunGauge,

        ExtraAttack,

        // Adds bonus damage to every Extra Attack triggered by this unit.
        // effect.value = additional damage per Extra Attack.
        ExtraAttackDamageUp,

        // Queues Extra Attacks for the next Attack cards actually played.
        // effect.value = Extra Attack base damage.
        // effect.repeat = number of Attack cards that will trigger it.
        ExtraAttackOnNextAttackCards,

        // Reduces the cost of random cards currently in hand for this turn.
        // effect.value = amount reduced from each selected card.
        // effect.repeat = number of distinct random cards selected.
        ReduceRandomHandCostThisTurn,

        // Every real card played after this effect resolves during the current
        // player turn triggers an Extra Attack.
        // effect.value = Extra Attack base damage.
        ExtraAttackEveryCardThisTurn,

        // For a limited duration, every Extra Attack triggered by this unit
        // fills its target's Stun gauge by additional percentage points.
        // effect.value = Stun gauge percentage points.
        // effect.statusDuration = duration in turns.
        ExtraAttackStunGaugeUp,

        // Adds bonus damage to every Follow-Up attack triggered by this unit
        // for a limited duration.
        // effect.value = additional Follow-Up damage.
        // effect.statusDuration = duration in turns.
        FollowUpDamageUp,

        // Adds bonus damage only to Follow-Up attacks triggered by consuming Mark.
        // effect.value = additional Mark Follow-Up damage.
        // effect.statusDuration = duration in turns.
        MarkFollowUpDamageUp,

        ConsumeMark,

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
        DoubleStress,

        TriggerShock
    }
}
