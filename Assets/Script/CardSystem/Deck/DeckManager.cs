using System.Collections.Generic;
using Steamchasing.Cards;
using UnityEngine;

namespace Steamchasing.DeckSystem
{
    public class DeckManager : MonoBehaviour
    {
        [SerializeField] private CardDatabase database;
        [SerializeField] private int startingHandSize = 5;

        public Deck RuntimeDeck { get; private set; } = new();

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError("DeckManager: CardDatabase is not assigned.", this);
                return;
            }

            database.Initialize();
        }

        public void BuildDeck(IEnumerable<string> cardIDs)
        {
            RuntimeDeck = new Deck();

            if (database == null)
            {
                Debug.LogError("DeckManager: Cannot build deck without a CardDatabase.", this);
                return;
            }

            foreach (string id in cardIDs)
            {
                CardData data = database.GetCard(id);
                if (data != null)
                    RuntimeDeck.AddToDrawPile(new CardInstance(data));
            }

            Shuffle(RuntimeDeck.DrawPile);
        }

        public void StartTurn()
        {
            Draw(startingHandSize);
        }

        public void Draw(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (RuntimeDeck.DrawPile.Count == 0)
                    ReshuffleDiscard();

                if (RuntimeDeck.DrawPile.Count == 0)
                    return;

                int last = RuntimeDeck.DrawPile.Count - 1;
                CardInstance card = RuntimeDeck.DrawPile[last];
                RuntimeDeck.DrawPile.RemoveAt(last);
                RuntimeDeck.AddToHand(card);
            }
        }

        /// <summary>
        /// Called after a card has successfully resolved.
        /// Normal cards go to discard. Disintegrated cards are removed from
        /// circulation for the rest of this combat by moving to ExhaustPile.
        /// </summary>
        public void ResolvePlayedCard(CardInstance card, bool disintegrate)
        {
            if (card == null)
                return;

            if (disintegrate)
                DisintegrateCard(card);
            else
                DiscardCard(card);
        }

        public void DiscardCard(CardInstance card)
        {
            if (card == null)
                return;

            RuntimeDeck.RemoveFromActivePiles(card);
            card.ClearTemporaryModifiers();

            if (!RuntimeDeck.DiscardPile.Contains(card))
                RuntimeDeck.AddToDiscard(card);
        }

        public void DisintegrateCard(CardInstance card)
        {
            if (card == null)
                return;

            RuntimeDeck.RemoveFromActivePiles(card);
            card.ClearTemporaryModifiers();

            if (!RuntimeDeck.ExhaustPile.Contains(card))
                RuntimeDeck.Exhaust(card);
        }

        public void DiscardHand()
        {
            while (RuntimeDeck.Hand.Count > 0)
            {
                CardInstance card = RuntimeDeck.Hand[0];
                DiscardCard(card);
            }
        }

        /// <summary>
        /// Restores all Disintegrated cards when the current combat ends.
        /// Disintegrate is combat-only in the GDD, so these cards must become
        /// available again in the next encounter.
        /// </summary>
        public void RestoreDisintegratedCards()
        {
            while (RuntimeDeck.ExhaustPile.Count > 0)
            {
                CardInstance card = RuntimeDeck.ExhaustPile[0];
                RuntimeDeck.ExhaustPile.RemoveAt(0);
                card.ClearTemporaryModifiers();
                RuntimeDeck.AddToDiscard(card);
            }
        }

        private void ReshuffleDiscard()
        {
            RuntimeDeck.DrawPile.AddRange(RuntimeDeck.DiscardPile);
            RuntimeDeck.DiscardPile.Clear();
            Shuffle(RuntimeDeck.DrawPile);
        }

        private static void Shuffle(List<CardInstance> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
