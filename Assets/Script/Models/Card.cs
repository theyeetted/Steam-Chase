using System.Collections.Generic;
using UnityEngine;


public class Card 
{
    public string Title => data.name;

    public int Cost { get; private set; }

    public bool Disintegrate => data.Disintegrate;

    public string Description => data.description;

    public Effect MannualTargetEffect => data.MannualTargetEffect;
    public ManualTargetKind ManualTargetKind => data.ManualTargetKind;

    public List<AutoTargetEffect> OtherEffects => data.OtherTargetEffects;

    public Sprite Image => data.Image;

    public Character Character => data.Character;
    public Rarity Rarity => data.Rarity;
    public CardType Type => data.Type;

    private readonly CardData data;

    public Card(CardData data)
    {
        this.data = data;
        Cost = data.Cost;
    }
}
