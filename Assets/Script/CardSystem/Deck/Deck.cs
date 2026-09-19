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

        public void AddToDrawPile(CardInstance card) => DrawPile.Add(card);
        public void AddToDiscard(CardInstance card) => DiscardPile.Add(card);
        public void AddToHand(CardInstance card) => Hand.Add(card);
        public void Exhaust(CardInstance card) => ExhaustPile.Add(card);
    }
}
