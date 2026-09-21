using System.Collections.Generic;
using Steamchasing.Cards;

namespace Steamchasing.DeckSystem
{
    public class Deck
    {
        public readonly List<CardInstance> DrawPile = new();
        public readonly List<CardInstance> Hand = new();
        public readonly List<CardInstance> DiscardPile = new();
        public readonly List<CardInstance> ExhaustPile = new();

        // Steamchasing calls the exhaust behavior "Disintegrate".
        // Keeping ExhaustPile preserves compatibility with the existing architecture.
        public IReadOnlyList<CardInstance> DisintegratedPile => ExhaustPile;

        public void AddToDrawPile(CardInstance card) => DrawPile.Add(card);
        public void AddToDiscard(CardInstance card) => DiscardPile.Add(card);
        public void AddToHand(CardInstance card) => Hand.Add(card);
        public void Exhaust(CardInstance card) => ExhaustPile.Add(card);

        public bool RemoveFromActivePiles(CardInstance card)
        {
            if (card == null)
                return false;

            bool removed = false;
            removed |= DrawPile.Remove(card);
            removed |= Hand.Remove(card);
            removed |= DiscardPile.Remove(card);
            return removed;
        }
    }
}