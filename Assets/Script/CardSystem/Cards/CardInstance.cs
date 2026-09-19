using System;
using UnityEngine;

namespace Steamchasing.Cards
{
    [Serializable]
    public class CardInstance
    {
        [SerializeField] private CardData data;
        [SerializeField] private bool upgraded;
        [SerializeField] private int temporaryCostModifier;

        public CardData Data => data;
        public bool Upgraded => upgraded;
        public int CurrentCost => Mathf.Max(0, data.EnergyCost + temporaryCostModifier);

        public CardInstance(CardData data)
        {
            this.data = data;
        }

        public void SetUpgraded(bool value) => upgraded = value;
        public void AddTemporaryCostModifier(int amount) => temporaryCostModifier += amount;
        public void ClearTemporaryModifiers() => temporaryCostModifier = 0;
    }
}
