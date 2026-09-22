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

        // Cost changes that are guaranteed to expire when the current turn ends.
        // Kept separate so clearing Overdrive Engine does not wipe unrelated
        // temporary modifiers from other systems.
        [SerializeField] private int turnCostModifier;

        public CardData Data => data;
        public bool Upgraded => upgraded;
        public int CurrentCost => Mathf.Max(
            0,
            data.EnergyCost +
            temporaryCostModifier +
            turnCostModifier
        );

        public CardInstance(CardData data)
        {
            this.data = data;
        }

        public void SetUpgraded(bool value) => upgraded = value;

        public void AddTemporaryCostModifier(int amount) =>
            temporaryCostModifier += amount;

        public void ClearTemporaryModifiers() =>
            temporaryCostModifier = 0;

        public void AddTurnCostModifier(int amount) =>
            turnCostModifier += amount;

        public void ClearTurnCostModifiers() =>
            turnCostModifier = 0;
    }
}
