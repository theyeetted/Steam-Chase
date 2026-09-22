using Steamchasing.Combat;
using System;
using UnityEngine;

namespace Steamchasing.Cards
{
    public enum EffectTarget
    {
        Self,
        SelectedTarget,
        AllAllies,
        AllEnemies,
        RandomEnemy
    }

    public enum CardConditionType
    {
        None,
        HasStatus,
        DoesNotHaveStatus,
        IsStunned,
        WasAttackedThisTurn,

        // True after this card's source has triggered at least one Extra Attack
        // during the current player turn.
        ExtraAttackTriggeredThisTurn
    }

    public enum ConditionSubject
    {
        Self,
        SelectedTarget,
        AnyEnemy
    }

    [Serializable]
    public class CardCondition
    {
        public CardConditionType type = CardConditionType.None;

        [Header("Who is checked?")]
        public ConditionSubject subject = ConditionSubject.SelectedTarget;

        [Header("Used by HasStatus / DoesNotHaveStatus")]
        public StatusEffectType statusType;

        [Min(1)]
        public int minimumStacks = 1;
    }

    [Serializable]
    public class CardEffect
    {
        public EffectType type;

        [Header("Target")]
        public EffectTarget target = EffectTarget.SelectedTarget;

        [Header("Value")]
        [Tooltip("Damage, Block, Draw amount, status stacks, etc.")]
        public int value = 1;

        [Tooltip("Number of separate times this effect resolves. Useful for multi-hit attacks.")]
        [Min(1)]
        public int repeat = 1;

        [Tooltip("Chance for each repetition to resolve. 1 = 100%, 0.5 = 50%.")]
        [Range(0f, 1f)]
        public float chance = 1f;

        [Tooltip("Duration used by stored statuses. -1 means use the status' normal/default duration rules.")]
        public int statusDuration = -1;

        [Header("Condition")]
        public CardCondition condition = new CardCondition();
    }
}