using System.Collections.Generic;
using UnityEngine;

namespace Steamchasing.Cards
{
    [CreateAssetMenu(fileName = "Card_", menuName = "Steamchasing/Cards/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string cardName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private Sprite artwork;

        [Header("Classification")]
        [SerializeField] private CardType cardType;
        [SerializeField] private CardRarity rarity;
        [SerializeField] private CharacterID character;
        [SerializeField] private DeckType deck;

        [Header("Cost")]
        [SerializeField, Range(0, 3)] private int energyCost;

        [Header("Availability")]
        [SerializeField] private bool isDraftable = true;
        [SerializeField] private bool isRemovable = true;

        [Header("Effects")]
        [SerializeField] private List<CardEffect> effects = new();

        public string ID => id;
        public string CardName => cardName;
        public string Description => description;
        public Sprite Artwork => artwork;
        public CardType CardType => cardType;
        public CardRarity Rarity => rarity;
        public CharacterID Character => character;
        public DeckType Deck => deck;
        public int EnergyCost => energyCost;
        public bool IsDraftable => isDraftable;
        public bool IsRemovable => isRemovable;
        public IReadOnlyList<CardEffect> Effects => effects;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(id))
                id = id.Trim();
            if (energyCost < 0) energyCost = 0;
            if (energyCost > 3) energyCost = 3;

            if (rarity == CardRarity.Unique)
                isDraftable = false;
        }
#endif
    }
}
