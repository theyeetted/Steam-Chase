using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Steamchasing.Cards
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Steamchasing/Cards/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        [SerializeField] private List<CardData> cards = new();

        private Dictionary<string, CardData> lookup;

        public IReadOnlyList<CardData> Cards => cards;

        public void Initialize()
        {
            lookup = new Dictionary<string, CardData>();

            foreach (var card in cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.ID))
                    continue;

                if (!lookup.TryAdd(card.ID, card))
                    Debug.LogError($"Duplicate Card ID: {card.ID}", card);
            }
        }

        public CardData GetCard(string id)
        {
            if (lookup == null) Initialize();

            if (lookup.TryGetValue(id, out var card))
                return card;

            Debug.LogError($"Card not found: {id}");
            return null;
        }

        public List<CardData> GetDraftable(CardRarity rarity)
        {
            return cards.Where(c => c != null &&
                                    c.IsDraftable &&
                                    c.Rarity == rarity)
                        .ToList();
        }

        public List<CardData> GetCharacterCards(CharacterID character, bool draftableOnly = false)
        {
            return cards.Where(c => c != null &&
                                    c.Character == character &&
                                    (!draftableOnly || c.IsDraftable))
                        .ToList();
        }

        public List<CardData> GetDeckCards(CharacterID character, DeckType deck, bool draftableOnly = false)
        {
            return cards.Where(c => c != null &&
                                    c.Character == character &&
                                    c.Deck == deck &&
                                    (!draftableOnly || c.IsDraftable))
                        .ToList();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();

            foreach (var card in cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.ID))
                    continue;

                if (!seen.Add(card.ID))
                    Debug.LogError($"Duplicate Card ID in database: {card.ID}", this);
            }
        }
#endif
    }
}
