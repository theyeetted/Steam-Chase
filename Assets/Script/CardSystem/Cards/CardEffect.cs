using System;
using UnityEngine;

namespace Steamchasing.Cards
{
    [Serializable]
    public class CardEffect
    {
        public EffectType type;
        public TargetType target;

        [Min(0)] public int value;
        [Min(0)] public int secondaryValue;

        [Range(0f, 1f)] public float chance = 1f;

        public bool disintegrateAfterUse;
    }
}
