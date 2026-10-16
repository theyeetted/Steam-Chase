using UnityEngine;
using System.Collections.Generic;
using SerializeReferenceEditor;

[CreateAssetMenu(menuName = "Data/CardData")]

public class CardData : ScriptableObject
{
    [field: SerializeField] public string description { get; private set; }

    [field: SerializeField] public Sprite Image { get; private set; }

    [field: SerializeField] public int Cost { get; private set; }

    [field: SerializeField] public Rarity Rarity { get; private set; }

    [field: SerializeField] public Character Character { get; private set; }

    [field: SerializeField, Tooltip("After this card is played, remove it from this combat instead of discarding it.")]
    public bool Disintegrate { get; private set; }

    [field: SerializeReference, SR] public Effect MannualTargetEffect { get; private set; } = null;

    [field: SerializeReference, SR] public List<AutoTargetEffect> OtherTargetEffects { get; private set; }
}

public enum Rarity { Base, Common, Rare, Legendary }

public enum Character { Whiskey, Ade }
