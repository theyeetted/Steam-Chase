using System;
using UnityEngine;

namespace Steamchasing.Cards
{
    [Serializable]
    public class CardEffect
    {
        [Tooltip("Gameplay effect executed when this card is played.")]
        public EffectType type;

        public TargetType target;

        [Min(0)] public int value;
        [Min(0)] public int secondaryValue;

        [Range(0f, 1f)] public float chance = 1f;
    }
}
