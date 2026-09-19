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
            database.Initialize();
        }

        public void BuildDeck(IEnumerable<string> cardIDs)
        {
            RuntimeDeck = new Deck();

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

        public void DiscardHand()
        {
            while (RuntimeDeck.Hand.Count > 0)
            {
                CardInstance card = RuntimeDeck.Hand[0];
                RuntimeDeck.Hand.RemoveAt(0);
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
